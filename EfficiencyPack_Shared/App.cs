#region Namespaces
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.Drawing;
using PushButton = Autodesk.Revit.UI.PushButton;
using SplitButton = Autodesk.Revit.UI.SplitButton;

#endregion

namespace EfficiencyPack
{
    public class IconSelector
    {
        // Method to select the appropriate icon based on the condition
        public static Bitmap SelectIcon(bool condition, string iconName)
        {
            // Load the resource based on the condition
            string resourceName = condition ? iconName + "_dark" : iconName;
            return (Bitmap)EfficiencyPack_Resources.Properties.Resources.ResourceManager.GetObject(resourceName);
        }
    }
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            // 1. Create ribbon tab
            try
            {
                app.CreateRibbonTab("Efficiency Pack");
            }
            catch (Exception)
            {
                Debug.Print("Tab already exists.");
            }
            bool dark = false;

            if (UIThemeManager.CurrentTheme == UITheme.Dark)
            {
                dark = true;
            }
            // 2. Create ribbon panel 
            RibbonPanel panel000 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "000 - Misc");
            RibbonPanel panel100 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "100 - Plans");
            RibbonPanel panel600 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "600 - Doors");
            RibbonPanel panel800 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "800 - Enlarged Drawings");
            RibbonPanel panel900 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "900 - Finishes");

            // 3. Create button data instances
            ButtonDataClass BtnForestGen = new ButtonDataClass("ForestGen", "Forest \rGenerator", ForestGen.GetMethod(), IconSelector.SelectIcon(dark, "forrest_32"), IconSelector.SelectIcon(dark, "forrest_16"), "Draw detail lines into a polygonal shape for your trees to be placed inside. Follow the command prompts to define amount and type.");
            ButtonDataClass BtnFloorByRoom = new ButtonDataClass("FloorByRoom", "Floors by \rRooms", FloorByRoom.GetMethod(), IconSelector.SelectIcon(dark, "floor_32"), IconSelector.SelectIcon(dark, "floor_16"), "Select rooms. Run command and follow prompt to select type of floor to populate.");
            ButtonDataClass BtnRoomPlanGen = new ButtonDataClass("RoomPlanGen", "Plans by \rRoom", RoomPlanGen.GetMethod(), IconSelector.SelectIcon(dark, "house_32"), IconSelector.SelectIcon(dark, "house_16"), "Select rooms. Run command. Get Plans.");
            ButtonDataClass BtnCenterRoom = new ButtonDataClass("CenterRoom", "Center\rRoom", RoomTagCenteringCommand.GetMethod(), IconSelector.SelectIcon(dark, "center_32"), IconSelector.SelectIcon(dark, "center_16"), "Select rooms. Center rooms.");
            ButtonDataClass BtnCenterRoomTag = new ButtonDataClass("CenterRoomTag", "Center\rRoom tag", RoomTagCenteringCommandTag.GetMethod(), IconSelector.SelectIcon(dark, "center_32"), IconSelector.SelectIcon(dark, "center_16"), "Select rooms. Center tags.");
            ButtonDataClass BtnRenameView = new ButtonDataClass("RenameView", "Rename\rView", RenameView.GetMethod(), IconSelector.SelectIcon(dark, "rename_32"), IconSelector.SelectIcon(dark, "rename_16"), "Select room. Rename View.");
            ButtonDataClass BtnDoorFireRating = new ButtonDataClass("DoorFireRating", "Door Fire\rRating", DoorFireRating.GetMethod(), IconSelector.SelectIcon(dark, "burning_32"), IconSelector.SelectIcon(dark, "burning_16"), "Select doors to set their fire rating.");
            ButtonDataClass BtnDoorCMUGWB = new ButtonDataClass("DoorCMU/GWB", "Door Inset\rOr Wrapped", DoorInsetWrap.GetMethod(), IconSelector.SelectIcon(dark, "door_32"), IconSelector.SelectIcon(dark, "door_16"), "Select doors to set whether they are inset or wrapped.");
            ButtonDataClass BtnDoorStorefrontMark = new ButtonDataClass("DoorStorefrontMark", "Door Storefront\rFrame Type", DoorStorefrontMark.GetMethod(), IconSelector.SelectIcon(dark, "window_32"), IconSelector.SelectIcon(dark, "window_16"), "Select storefront or curtainwall doors. Get mark based on the storefront mark.");
            ButtonDataClass BtnInteriorElevation = new ButtonDataClass("InteriorElevation", "Make Interior\rElevations", InteriorElevation.GetMethod(), IconSelector.SelectIcon(dark, "entrance_32"), IconSelector.SelectIcon(dark, "entrance_16"), "Select rooms to elevate.");
            ButtonDataClass BtnModifyCrop = new ButtonDataClass("ModifyCropBoundaryCommand", "Raise Crop\rBoundary", ModifyCropBoundaryCommand.GetMethod(), IconSelector.SelectIcon(dark, "elevation_32"), IconSelector.SelectIcon(dark, "elevation_16"), "Raises the crop boundary of selected views");
            ButtonDataClass BtnFilledRegionDonut = new ButtonDataClass("FilledRegionDonut", "Create\rDonut", CreateFilledRegionCommand.GetMethod(), IconSelector.SelectIcon(dark, "donut_32"), IconSelector.SelectIcon(dark, "donut_16"), "Select views. Get Donuts.");
            ButtonDataClass BtnDimensionOverride = new ButtonDataClass("DimensionText", "Override Dim", DimensionText.GetMethod(), IconSelector.SelectIcon(dark, "width_32"), IconSelector.SelectIcon(dark, "width_16"), "Override the dimension Value with EQ");
            ButtonDataClass BtnLabelOffset = new ButtonDataClass("LabelOffset", "Modify Label\rLength", ModifyLabelOffset.GetMethod(), IconSelector.SelectIcon(dark, "length_32"), IconSelector.SelectIcon(dark, "length_16"), "Updates the label offset to the length of the view title");
            ButtonDataClass BtnExplodeCAD = new ButtonDataClass("ExplodeCAD", "Explode CAD", ExplodeCAD.GetMethod(), IconSelector.SelectIcon(dark, "house_32"), IconSelector.SelectIcon(dark, "house_16"), "Explodes CAD based on the selected line styles");
            ButtonDataClass BtnImportTypes = new ButtonDataClass("ImportTypes", "Import types", ImportTypes.GetMethod(), IconSelector.SelectIcon(dark, "cabinet_32"), IconSelector.SelectIcon(dark, "cabinet_16"), "Imports only selected types from reference project.");
            ButtonDataClass BtnCenterCeilingGrid = new ButtonDataClass("CenterCeilingGrid", "Center Ceiling\rGrid", CenterCeilingGrid.GetMethod(), IconSelector.SelectIcon(dark, "center_32"), IconSelector.SelectIcon(dark, "center_16"), "Centers the ceiling grid in the room. Dimensions EQ.");
            ButtonDataClass BtnGoogleEarthTopo = new ButtonDataClass("GoogleEarthTopo", "Google Earth\rTopo", GoogleEarthTopo.GetMethod(), IconSelector.SelectIcon(dark, "site_32"), IconSelector.SelectIcon(dark, "site_16"), "Creates a topography based on a region defined in google earth.");
            ButtonDataClass BtnAlignViewsOnSheets = new ButtonDataClass("AlignViewsOnSheets", "Align Views\rOn Sheets", AlignViewsOnSheets.GetMethod(), IconSelector.SelectIcon(dark, "center_32"), IconSelector.SelectIcon(dark, "center_16"), "Aligns views across sheets. Select original view, active the tool, then select what you want aligned to the original");
            ButtonDataClass BtnConfetti = new ButtonDataClass("Deadline", "Deadline", Confetti.GetMethod(), IconSelector.SelectIcon(dark, "cracker_32"), IconSelector.SelectIcon(dark, "cracker_16"), "Push after deadline");
            ButtonDataClass BtnHelpApp = new ButtonDataClass("Help", "Help", HelpApp.GetMethod(), IconSelector.SelectIcon(dark, "question_32"), IconSelector.SelectIcon(dark, "question_16"), "Push for tool guide");
            ButtonDataClass BtnWallByRoom = new ButtonDataClass("WallsByRoom", "Walls by \rRooms", WallsByRoom.GetMethod(), IconSelector.SelectIcon(dark, "floor_32"), IconSelector.SelectIcon(dark, "floor_16"), "Select rooms. Run command and follow prompt to select type of Wals to populate.");

            // 4. Create buttons
            PushButton myButton20 = panel000.AddItem(BtnHelpApp.Data) as PushButton;
            PushButton myButton16 = panel000.AddItem(BtnExplodeCAD.Data) as PushButton;
            PushButton myButton19 = panel100.AddItem(BtnCenterCeilingGrid.Data) as PushButton;
            PushButton myButton6 = panel800.AddItem(BtnRoomPlanGen.Data) as PushButton;
            PushButton myButton10 = panel800.AddItem(BtnInteriorElevation.Data) as PushButton;
            PushButton myButton14 = panel800.AddItem(BtnDimensionOverride.Data) as PushButton;
#if REVIT2023|| REVIT2024
            PushButton myButton17 = panel000.AddItem(BtnGoogleEarthTopo.Data) as PushButton;
#endif

            //5. Split buttons
            SplitButtonData splitButtonData = new SplitButtonData("Center Room", "Center\rRoom");
            SplitButton splitButton = panel000.AddItem(splitButtonData) as SplitButton;
            splitButton.AddPushButton(BtnCenterRoom.Data);
            splitButton.AddPushButton(BtnCenterRoomTag.Data);
            SplitButtonData splitButtonData3 = new SplitButtonData("View Modifiers", "View\rModifier");
            SplitButton splitButton3 = panel800.AddItem(splitButtonData3) as SplitButton;
            splitButton3.AddPushButton(BtnModifyCrop.Data);
            splitButton3.AddPushButton(BtnFilledRegionDonut.Data);
            SplitButtonData splitButtonData2 = new SplitButtonData("Door Modifiers", "Door\rModifier");
            SplitButton splitButton2 = panel600.AddItem(splitButtonData2) as SplitButton;
            splitButton2.AddPushButton(BtnDoorCMUGWB.Data);
            splitButton2.AddPushButton(BtnDoorStorefrontMark.Data);
            SplitButtonData splitButtonData4 = new SplitButtonData("Finishes", "Finishes");
            SplitButton splitButton4 = panel900.AddItem(splitButtonData4) as SplitButton;
            splitButton4.AddPushButton(BtnFloorByRoom.Data);
            splitButton4.AddPushButton(BtnWallByRoom.Data);


            //6. stack buttons
            panel000.AddStackedItems(BtnImportTypes.Data, BtnForestGen.Data, BtnConfetti.Data);
            panel000.AddStackedItems(BtnLabelOffset.Data, BtnRenameView.Data, BtnAlignViewsOnSheets.Data);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
