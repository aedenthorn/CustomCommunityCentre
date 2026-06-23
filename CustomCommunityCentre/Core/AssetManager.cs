using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomCommunityCentre
{
	public class AssetManager
    {
        private static IModHelper Helper => ModEntry.Instance.Helper;

        // Assets
        public static string AssetPrefix => CustomCommunityCentre.ModEntry.Instance.ModManifest.UniqueID;
		public static readonly char[] ForbiddenAssetNameCharacters = new char[]
		{
			System.IO.Path.DirectorySeparatorChar,
			Bundles.ModDataKeyDelim,
			Bundles.ModDataValueDelim
		};
		public const string RequiredAssetNamePrefix = "Custom";
		public const char RequiredAssetNameDivider = '_';
		public static readonly string RootGameContentPath = PathUtilities.NormalizeAssetName(
			$@"Mods/{CustomCommunityCentre.ModEntry.Instance.ModManifest.UniqueID}.Assets");

		// Content pack assets
		public static string ContentPackDataFileName { get; private set; } = "content";
		public static string TemporaryManifestFileName { get; private set; } = "content-pack";

		// Internal sneaky asset business
		internal static readonly string BundleCacheAssetKey = CustomCommunityCentre.AssetManager.PrefixAsset(
			asset: "BundleCacheAssetkey", prefix: CustomCommunityCentre.AssetManager.RootGameContentPath, separator: "/");

		// Asset dictionary keys
		public const string BundleMetadataKey = "Metadata";
		public const string BundleDefinitionsKey = "Definitions";
		public const string BundleSubstitutesKey = "Substitutes";

		// Asset lists
		public static readonly List<string> GameAssetKeys = new()
        {
			@"Data/Events/Town",
			@"Strings/BundleNames",
			@"Strings/Locations",
			@"Strings/UI",
		};


        internal static void RegisterEvents()
        {
            Helper.Events.Content.AssetRequested += AssetManager.Content_AssetRequested;
        }

        private static void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
			// Internal sneaky asset business
			if (e.NameWithoutLocale.IsEquivalentTo(CustomCommunityCentre.AssetManager.BundleCacheAssetKey))
			{
				e.LoadFrom(() => BundleManager.Parse(), StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
                return;
            }


            // Load content pack spritesheets when referenced by the game
            Texture2D spritesheet = CustomCommunityCentre.ModEntry.ContentPacks
				.Find(cp => cp.Spritesheets.Keys.Any(s => e.NameWithoutLocale.IsEquivalentTo(s)))
				.Spritesheets
				.First(pair => e.NameWithoutLocale.IsEquivalentTo(pair.Key)).Value;
			if (spritesheet != null)
			{
				e.LoadFrom(() => spritesheet, StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
				return;
			}
			if (ModEntry.ContentPacks == null || !ModEntry.ContentPacks.Any())
				return;

            IEnumerable<Data.BundleMetadata> bundleMetadata = Bundles.GetAllCustomBundleMetadataEntries();

			if (e.NameWithoutLocale.IsEquivalentTo(@"Data/Events/Town"))
			{
				e.Edit((IAssetData asset) =>
				{
                    var data = asset.AsDictionary<string, string>().Data;

                    // Append completed mail received for all custom areas as required flags for CC completion event

                    const char delimiter = '/';
                    const string mailFlag = "Hn";

                    string eventId = ((int)Bundles.EventIds.CommunityCentreComplete).ToString();
                    string eventKey = data.Keys.FirstOrDefault(key => key.Split(delimiter).First() == eventId);
                    string eventScript = data[eventKey];
                    string[] mailFlags = new List<string> { eventKey }
                        .Concat(Bundles.CustomAreaNamesAndNumbers.Keys
                            .Select(areaName => $"{mailFlag} {string.Format(Bundles.MailAreaCompleted, Bundles.GetAreaNameAsAssetKey(areaName))}"))
                        .ToArray();

                    data.Remove(eventKey);
                    eventKey = string.Join(delimiter.ToString(), mailFlags);
                    data[eventKey] = eventScript;

                });
				return;
			}
            if (e.NameWithoutLocale.IsEquivalentTo(@"Strings/BundleNames"))
            {
				e.Edit((IAssetData asset) =>
				{
                    var data = asset.AsDictionary<string, string>().Data;

                    // Add bundle display names to localised bundle names dictionary
                    foreach (CustomCommunityCentre.Data.BundleMetadata bmd in bundleMetadata)
                    {
                        foreach (string bundleName in bmd.BundleDisplayNames.Keys)
                        {
                            data[bundleName] = CustomCommunityCentre.Data.BundleMetadata.GetLocalisedString(
                                dict: bmd.BundleDisplayNames[bundleName],
                                defaultValue: bundleName);
                        }
                    }

                });
				return;
			}
            if (e.NameWithoutLocale.IsEquivalentTo(@"Strings/Locations"))
            {
                e.Edit((IAssetData asset) =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    // Add area display names and completion strings
                    foreach (CustomCommunityCentre.Data.BundleMetadata bmd in bundleMetadata)
                    {
                        string areaNameAsAssetKey = Bundles.GetAreaNameAsAssetKey(bmd.AreaName);
                        data[$"CommunityCenter_AreaName_{areaNameAsAssetKey}"] = bmd.AreaDisplayName
                            .TryGetValue(LocalizedContentManager.CurrentLanguageCode.ToString(), out string str)
                            ? str
                            : bmd.AreaName;

                        str = CustomCommunityCentre.Data.BundleMetadata.GetLocalisedString(dict: bmd.AreaCompleteDialogue, defaultValue: string.Empty);
                        data[$"CommunityCenter_AreaCompletion_{areaNameAsAssetKey}"] = str;
                    }

                });

				return;
			}
            if (e.NameWithoutLocale.IsEquivalentTo(@"Strings/UI"))
            {
                e.Edit((IAssetData asset) =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    // Add reward text
                    foreach (CustomCommunityCentre.Data.BundleMetadata bmd in bundleMetadata)
                    {
                        int areaNumber = Bundles.GetCustomAreaNumberFromName(bmd.AreaName);
                        data[$"JunimoNote_Reward{areaNumber}"] = bmd.AreaCompleteDialogue
                            .TryGetValue(LocalizedContentManager.CurrentLanguageCode.ToString(), out string str)
                            ? str
                            : string.Empty;
                    }

                });
            }
		}

		public static void ReloadAssets(IModHelper helper)
		{
			// Invalidate game assets
			helper.GameContent.InvalidateCache(@"Strings/UI");
		}

		public static string PrefixAsset(string asset, string prefix = null, string separator = ".")
		{
			return PathUtilities.NormalizeAssetName(string.Join(separator,
				prefix ?? CustomCommunityCentre.AssetManager.AssetPrefix, asset));
		}

		public static string PrefixPath(string asset, string prefix = null, string separator = "/")
		{
			return PathUtilities.NormalizePath(string.Join(separator,
				prefix ?? CustomCommunityCentre.AssetManager.RootGameContentPath, asset));
		}
	}
}
