﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

namespace LDtkUnity.Editor
{
    /// <summary>
    /// This clones assets so that the exported prefab has it's independent assets set up. once generated, then it can be used to replace the assets in the prefab factory
    /// </summary>
    internal class LDtkNativePrefabAssets
    {
        private readonly LDtkProjectImporter _importer;
        private readonly LDtkArtifactAssets _assets;
        private readonly string _path;
        private readonly Sprite _oldSprite;
        private readonly Texture2D _newTexture;
        
        private List<Sprite> _artTileSprites = new List<Sprite>();
        private List<Tile> _artTiles = new List<Tile>();
        private List<Tile> _intGridTiles = new List<Tile>();
        private List<Sprite> _backgroundArtifacts = new List<Sprite>();
        
        public List<Tile> ArtTiles => _artTiles.ToList();
        public List<Tile> IntGridTiles => _intGridTiles.ToList();
        public List<Sprite> BackgroundArtifacts => _backgroundArtifacts.ToList();

        public LDtkNativePrefabAssets(LDtkProjectImporter importer, LDtkArtifactAssets assets, string path)
        {
            _importer = importer;
            _path = path;
            _assets = assets;
            _oldSprite = LDtkResourcesLoader.LoadDefaultTileSprite();
            _newTexture = Texture2D.whiteTexture;
        }

        public void GenerateAssets()
        {
            if (_importer == null)
            {
                Debug.LogError("Null Importer");
                return;
            }
            
            if (_assets == null)
            {
                Debug.LogError("Null ArtifactAssets");
                return;
            }
            
            try
            {
                AssetDatabase.StartAssetEditing();
                MainAssetGeneration();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            
            //now that this is done, we can make the prefab factory replace the old ones with these newly created prefabs
            AssetDatabase.Refresh();
        }

        private void MainAssetGeneration()
        {
            LDtkIntGridTile oldDefaultTile = LDtkResourcesLoader.LoadDefaultTile();
            Tile newDefaultTile = CreateNativeTile(oldDefaultTile);
            
            //export default texture
            Texture2D newDefaultTexture = CloneArtifact(_newTexture, "/Sprites", _oldSprite.name + "Texture");
            
            //export the default sprite
            Sprite newDefaultSprite = Sprite.Create(newDefaultTexture, _oldSprite.rect, GetNormalizedPivotOfSprite(_oldSprite), _oldSprite.pixelsPerUnit, 1, SpriteMeshType.Tight, _oldSprite.border, true);
            newDefaultSprite = CloneArtifact(newDefaultSprite, "/Sprites", _oldSprite.name);
            
            //clone art tile sprites
            _artTileSprites = CloneArtifacts(_assets.SpriteArtifacts, "/Sprites");
            
            //clone art tiles
            _artTiles = CloneArtifacts(_assets.TileArtifacts, "/ArtTiles").Cast<Tile>().ToList();

            //clone int grid tiles
            List<TileBase> oldIntGridArtifacts = _importer.GetIntGridTiles().Where(p => p != null).Append(newDefaultTile).ToList();
            _intGridTiles = CloneArtifacts(oldIntGridArtifacts, "/IntGridValues").Cast<Tile>().ToList();

            //clone background sprites
            _backgroundArtifacts = CloneArtifacts(_assets.BackgroundArtifacts, "/Backgrounds");
            
            EditorApplication.delayCall += TryCloneSpriteAtlas;

            //give each new native art tile a matching cloned sprite to reference
            foreach (Tile artTile in _artTiles)
            {
                string nameMatch = artTile.name;
                Sprite oldArtTileSprite = _artTileSprites.Find(sprite => sprite.name == nameMatch);
                artTile.sprite = oldArtTileSprite;
            }
            
            //give the int grid tiles the clone sprite instead if they were using the default tile
            foreach (Tile intGridTile in _intGridTiles)
            {
                if (intGridTile.sprite == _oldSprite)
                {
                    intGridTile.sprite = newDefaultSprite;
                }
            } 
            
            //we've generated the default sprite which is used by multiple, simply add to the list since it's already created
            _backgroundArtifacts.Add(newDefaultSprite);
        }

        private void TryCloneSpriteAtlas()
        {
            SpriteAtlas atlas = _importer.Atlas;
            if (!atlas)
            {
                return;
            }
            
            //clone the sprite atlas asset
            SpriteAtlas newAtlas = CloneArtifact(atlas, "/Sprites");

            //clear, then re-fill the atlas with used sprites
            newAtlas.Remove(newAtlas.GetPackables());
            
            //add art tiles
            Object[] objects = _artTileSprites.OrderBy(p => p.name).Cast<Object>().ToArray();
            newAtlas.Add(objects);

            //pack
            SpriteAtlasUtility.PackAtlases(new []{newAtlas}, EditorUserBuildSettings.activeBuildTarget);
        }

        private T CloneArtifact<T>(T artifact, string extraPath, string assetName = null) where T : Object
        {
            return CloneArtifacts(new[] { artifact }.ToList(), extraPath, assetName).First();
        }
        
        private List<T> CloneArtifacts<T>(List<T> artifacts, string extraPath, string assetName = null) where T : Object
        {
            if (artifacts.IsNullOrEmpty())
            {
                return new List<T>();
            }
            
            string parentPath = $"{_path}{extraPath}";
            LDtkPathUtility.TryCreateDirectory(parentPath);

            List<T> list = new List<T>();
            foreach (T artifact in artifacts)
            {
                if (artifact == null)
                {
                    continue;
                }
                
                string cloneName = assetName != null ? assetName : artifact.name;
                string destinationPath = $"{parentPath}/{cloneName}.asset";

                //Debug.Log($"Copy asset\n{artifact.name}\nto\n{destinationPath}");

                Object clone = CreateClone(artifact);
                clone.name = cloneName;

                T loadedAsset = AssetDatabase.LoadAssetAtPath<T>(destinationPath);
                
                if (loadedAsset)
                {
                    EditorUtility.CopySerializedIfDifferent(clone, loadedAsset);
                    list.Add(loadedAsset);
                }
                else
                {
                    AssetDatabase.CreateAsset(clone, destinationPath);
                    list.Add((T)clone);
                }
            }

            return list;
        }

        //if it's an LDtk asset, then turn it into a native asset, otherwise if it's already native, then instantiate
        private Object CreateClone<T>(T artifact) where T : Object
        {
            //return a sprite in this way because instantiating a sprite that is packed to an atlas makes a unity error appear AssetDatabase.CreateAsset
            //return a sprite clone, and also make a sprite atlas asset somewhere
            if (artifact is Sprite oldSprite)
            {
                return Sprite.Create(oldSprite.texture, oldSprite.rect, GetNormalizedPivotOfSprite(oldSprite), oldSprite.pixelsPerUnit);
            }
            
            if (typeof(TileBase).IsAssignableFrom(typeof(T)))
            {
                return CreateNativeTile(artifact);
            }
            
            T clone = Object.Instantiate(artifact);
            return clone;
        }

        private Vector2 GetNormalizedPivotOfSprite(Sprite sprite)
        {
            return sprite.pivot / sprite.rect.size;
        }

        private static Tile CreateNativeTile<T>(T artifact) where T : Object
        {
            TileBase tile = artifact as TileBase;
            if (tile == null)
            {
                Debug.LogError("Tile not casted");
                return null;
            }

            TileData tileData = default;
            tile.GetTileData(default, null, ref tileData);
            
            Tile nativeTile = ScriptableObject.CreateInstance<Tile>();
            nativeTile.name = artifact.name;
            nativeTile.sprite = tileData.sprite;
            nativeTile.color = tileData.color;
            nativeTile.transform = tileData.transform;
            nativeTile.gameObject = tileData.gameObject;
            nativeTile.flags = tileData.flags;
            nativeTile.colliderType = tileData.colliderType;
            return nativeTile;
        }
    }
}