using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommunityKitchen
{
    public class AssetManager
    {
        private static IModHelper Helper => ModEntry.Instance.Helper;

        // Game content assets
        public static readonly string RootGameContentPath = PathUtilities.NormalizeAssetName(
			$@"Mods/{CommunityKitchen.ModEntry.Instance.ModManifest.UniqueID}.Assets");
		public static readonly string DeliverySpritesAssetKey = Path.Combine(RootGameContentPath, "DeliverySprites");

		// Local content assets
		internal static readonly string LocalDeliverySpritesPath = @"assets/deliverySprites";
		internal static readonly string KitchenContentPackPath = @"assets/[CCC] KitchenContentPack";

		// Asset lists
		public static readonly List<string> GameAssetKeys = new()
		{
			@"Maps/townInterior",
			@"Data/mail",
		};

        internal static void RegisterEvents()
        {
            Helper.Events.Content.AssetRequested += AssetManager.Content_AssetRequested;
        }

        private static void Content_AssetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
			if (e.NameWithoutLocale.IsEquivalentTo(CommunityKitchen.AssetManager.DeliverySpritesAssetKey))
			{
				e.LoadFromModFile<Texture2D>($"{CommunityKitchen.AssetManager.LocalDeliverySpritesPath}.png", StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
			}
			else if (e.NameWithoutLocale.IsEquivalentTo(@"Data/mail"))
			{
				e.Edit((IAssetData asset) =>
				{
                    var data = asset.AsDictionary<string, string>().Data;

                    // Append completed mail received for all custom areas as required flags for CC completion event

                    string mailId = string.Format(CustomCommunityCentre.Bundles.MailAreaCompletedFollowup, Kitchen.KitchenAreaName);
                    data[mailId] = CommunityKitchen.ModEntry.i18n.Get("mail.areacompletedfollowup.gus");

                    mailId = GusDeliveryService.MailSaloonDeliverySurchargeWaived;
                    data[mailId] = CommunityKitchen.ModEntry.i18n.Get("mail.saloondeliverysurchargewaived");

                });
			}
			else if (e.NameWithoutLocale.IsEquivalentTo(@"Maps/townInterior"))
			{
				if (!(Game1.currentLocation is StardewValley.Locations.CommunityCenter))
					return;
                e.Edit((IAssetData asset) =>
                {
                    var image = asset.AsImage();

                    // Openable fridge in the kitchen
                    Rectangle targetArea = Kitchen.FridgeOpenedSpriteArea; // Target some unused area of the sheet for this location
                    Rectangle sourceArea = new(320, 224, targetArea.Width, targetArea.Height); // Apply base fridge sprite
                    image.PatchImage(
                        source: image.Data,
                        sourceArea: sourceArea,
                        targetArea: targetArea,
                        patchMode: PatchMode.Replace);

                    sourceArea = new Rectangle(0, 192, 16, 32); // Patch in opened-door fridge sprite from mouseCursors sheet
                    image.PatchImage(
                        source: Game1.mouseCursors2,
                        sourceArea: sourceArea,
                        targetArea: targetArea,
                        patchMode: PatchMode.Overlay);

                    // New star on the community centre area tracker wall
                    sourceArea = new Rectangle(370, 705, 7, 7);
                    targetArea = new Rectangle(380, 710, sourceArea.Width, sourceArea.Height);
                    image.PatchImage(
                        source: image.Data,
                        sourceArea: sourceArea,
                        targetArea: targetArea,
                        patchMode: PatchMode.Replace);

                });

			}
		}
	}
}
