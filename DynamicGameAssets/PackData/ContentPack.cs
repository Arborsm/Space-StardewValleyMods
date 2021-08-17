﻿using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicGameAssets.PackData
{
    public class ContentPack
    {
        internal IContentPack smapiPack;

        internal SemanticVersion conditionVersion;

        internal Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        internal Dictionary<string, CommonPackData> items = new Dictionary<string, CommonPackData>();

        internal List<BasePackData> others = new List<BasePackData>();

        public ContentPack( IContentPack pack )
        {
            smapiPack = pack;
            conditionVersion = new SemanticVersion( pack.Manifest.ExtraFields[ "DGAConditionsFormatVersion" ].ToString() );
            LoadAndValidateItems<ObjectPackData>( "objects.json" );
            LoadAndValidateItems<CraftingPackData>("crafting.json");
            LoadAndValidateItems<FurniturePackData>("furniture.json");
            LoadOthers<ShopPackData>( "shop-entries.json" );
        }

        public CommonPackData Find( string item )
        {
            return items.ContainsKey( item ) ? items[ item ] : null;
        }

        private void LoadAndValidateItems< T >( string json ) where T : CommonPackData
        {
            if (!smapiPack.HasFile(json))
                return;

            var data = smapiPack.LoadAsset<List<T>>( json ) ?? new List<T>();
            foreach ( var d in data )
            {
                if ( items.ContainsKey( d.ID ) )
                    throw new ArgumentException( "Duplicate found! " + d.ID );
                items.Add( d.ID, d );
                Mod.itemLookup.Add( $"{smapiPack.Manifest.UniqueID}/{d.ID}".GetHashCode(), $"{smapiPack.Manifest.UniqueID}/{d.ID}" );
                d.parent = this;
                d.original = ( T ) d.Clone();
                d.original.original = d.original;
                d.PostLoad();
            }
        }

        private void LoadOthers<T>( string json ) where T : BasePackData
        {
            if (!smapiPack.HasFile(json))
                return;

            var data = smapiPack.LoadAsset<List<T>>( json ) ?? new List<T>();
            foreach ( var d in data )
            {
                others.Add( d );
                d.parent = this;
                d.original = ( T ) d.Clone();
                d.original.original = d.original;
                d.PostLoad();
            }
        }

        internal TexturedRect GetTexture( string path, int xSize, int ySize )
        {
            int colon = path.IndexOf( ':' );
            if (colon == -1 && !smapiPack.HasFile(path) || colon != -1 && !smapiPack.HasFile(path.Substring(0, colon)))
                throw new ArgumentException("No such file \"" + path + "\"!");
            if ( colon == -1 )
                return new TexturedRect() { Texture = smapiPack.LoadAsset< Texture2D >( path ), Rect = null };
            var tex = smapiPack.LoadAsset< Texture2D >( path.Substring( 0, colon ) );
            int sections = tex.Width / xSize;
            int ind = int.Parse( path.Substring( colon + 1 ) );
            return new TexturedRect()
            {
                Texture = tex,
                Rect = new Microsoft.Xna.Framework.Rectangle( ind % sections * xSize, ind / sections * ySize, xSize, ySize )
            };
        }
    }
}
