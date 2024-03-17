#region Namespaces
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;

#endregion

namespace EfficiencyPack
{
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

            // 2. Create ribbon panel 
            RibbonPanel panel000 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "000 - Misc");
            RibbonPanel panel600 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "600 - Doors");
            RibbonPanel panel800 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "800 - Enlarged Drawings");
            RibbonPanel panel900 = Utils.CreateRibbonPanel(app, "Efficiency Pack", "900 - Finishes");

            // 3. Create button data instances
            ButtonDataClass BtnForestGen = new ButtonDataClass("ForestGen", "Forest \rGenerator", ForestGen.GetMethod(), EfficiencyPack_Resources.Properties.Resources.forrest_32, EfficiencyPack_Resources.Properties.Resources.forrest_16, "Draw detail lines into a polygonal shape for your trees to be placed inside. Follow the command prompts to define amount and type.");
            ButtonDataClass BtnFloorByRoom = new ButtonDataClass("FloorByRoom", "Floors by \rRooms", FloorByRoom.GetMethod(), EfficiencyPack_Resources.Properties.Resources.floor_32, EfficiencyPack_Resources.Properties.Resources.floor_16, "Select rooms. Run command and follow prompt to select type of floor to populate.");
#if REVIT2022
            ButtonDataClass BtnDuplicateSheet = new ButtonDataClass("DuplicateSheet", "Duplicate \rSheet", DuplicateViewsCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.copy_32, EfficiencyPack_Resources.Properties.Resources.copy_16, "Select Sheet. Run command.");
#endif
            ButtonDataClass BtnFloorByDepartment = new ButtonDataClass("FloorByDepartment", "Floors by \rDepartment", FloorByDepartment.GetMethod(), EfficiencyPack_Resources.Properties.Resources.floor_32, EfficiencyPack_Resources.Properties.Resources.floor_16, "Select rooms. Run command. Get Floors.");
            ButtonDataClass BtnLinesByRoom = new ButtonDataClass("LinesByRoom", "Lines by \rRoom", LinesByRoom.GetMethod(), EfficiencyPack_Resources.Properties.Resources.outline_32, EfficiencyPack_Resources.Properties.Resources.outline_16, "Select rooms. Run command. Get Outline.");
            ButtonDataClass BtnRoomPlanGen = new ButtonDataClass("RoomPlanGen", "Plans by \rRoom", RoomPlanGen.GetMethod(), EfficiencyPack_Resources.Properties.Resources.house_32, EfficiencyPack_Resources.Properties.Resources.house_16, "Select rooms. Run command. Get Plans.");
            ButtonDataClass BtnCenterRoom = new ButtonDataClass("CenterRoom", "Center\rRoom", RoomTagCenteringCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.center_32, EfficiencyPack_Resources.Properties.Resources.center_16, "Select rooms. Center rooms.");
            ButtonDataClass BtnCenterRoomTag = new ButtonDataClass("CenterRoomTag", "Center\rRoom tag", RoomTagCenteringCommandTag.GetMethod(), EfficiencyPack_Resources.Properties.Resources.center_32, EfficiencyPack_Resources.Properties.Resources.center_16, "Select rooms. Center tags.");
            ButtonDataClass BtnRenameView = new ButtonDataClass("RenameView", "Rename\rView", RenameView.GetMethod(), EfficiencyPack_Resources.Properties.Resources.rename_32, EfficiencyPack_Resources.Properties.Resources.rename_16, "Select room. Rename View.");
            ButtonDataClass BtnDoorFireRating = new ButtonDataClass("DoorFireRating", "Door Fire\rRating", DoorFireRating.GetMethod(), EfficiencyPack_Resources.Properties.Resources.burning_32, EfficiencyPack_Resources.Properties.Resources.burning_16, "Select doors to set their fire rating.");
            ButtonDataClass BtnDoorCMUGWB = new ButtonDataClass("DoorCMU/GWB", "Door Inset\rOr Wrapped", DoorInsetWrap.GetMethod(), EfficiencyPack_Resources.Properties.Resources.door_32, EfficiencyPack_Resources.Properties.Resources.door_16, "Select doors to set whether they are inset or wrapped.");
            ButtonDataClass BtnDoorStorefrontMark = new ButtonDataClass("DoorStorefrontMark", "Door Storefront\rMark", DoorStorefrontMark.GetMethod(), EfficiencyPack_Resources.Properties.Resources.window_32, EfficiencyPack_Resources.Properties.Resources.window_16, "Select storefront or curtainwall doors. Get mark based on the storefront mark.");
            ButtonDataClass BtnStorefrontElevation = new ButtonDataClass("StorefrontElevation", "Make Storefront\rElevations", CurtainWallElevationAddIn.GetMethod(), EfficiencyPack_Resources.Properties.Resources.window_32, EfficiencyPack_Resources.Properties.Resources.window_16, "Select storefront or curtainwall to create elevations of them.");
            ButtonDataClass BtnInteriorElevation = new ButtonDataClass("InteriorElevation", "Make Interior\rElevations", InteriorElevation.GetMethod(), EfficiencyPack_Resources.Properties.Resources.entrance_32, EfficiencyPack_Resources.Properties.Resources.entrance_16, "Select rooms to elevate.");
            ButtonDataClass BtnModifyCrop = new ButtonDataClass("ModifyCropBoundaryCommand", "Raise Crop\rBoundary", ModifyCropBoundaryCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.elevation_32, EfficiencyPack_Resources.Properties.Resources.elevation_16, "Raises the crop boundary of selected views");
            ButtonDataClass BtnFilledRegionDonut = new ButtonDataClass("FilledRegionDonut", "Create\rDonut", CreateFilledRegionCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.donut_32, EfficiencyPack_Resources.Properties.Resources.donut_16, "Select views. Get Donuts.");
            ButtonDataClass BtnSetTypeImageCommand = new ButtonDataClass("SetTypeImageCommand", "Set new\rType Image", SetTypeImageCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.cabinet_32, EfficiencyPack_Resources.Properties.Resources.cabinet_16, "Select Family. Input Type Image.");
            ButtonDataClass BtnWorkingViews = new ButtonDataClass("WorkingViews", "Create\rworking views", WorkingViews.GetMethod(), EfficiencyPack_Resources.Properties.Resources.binoculars_32, EfficiencyPack_Resources.Properties.Resources.binoculars_16, "Create a set\rof new working views for you");
            ButtonDataClass BtnDimensionOverride = new ButtonDataClass("DimensionText", "Override Dim", DimensionText.GetMethod(), EfficiencyPack_Resources.Properties.Resources.width_32, EfficiencyPack_Resources.Properties.Resources.width_16, "Override the dimension Value with EQ");
            ButtonDataClass BtnLabelOffset = new ButtonDataClass("LabelOffset", "Modify Label\rLength", ModifyLabelOffset.GetMethod(), EfficiencyPack_Resources.Properties.Resources.length_32, EfficiencyPack_Resources.Properties.Resources.length_16, "Updates the label offset to the length of the view title");
            ButtonDataClass BtnImportViewType = new ButtonDataClass("ViewTypeImport", "Import View\rTypes", ViewTypeImportTool.GetMethod(), EfficiencyPack_Resources.Properties.Resources.binoculars_32, EfficiencyPack_Resources.Properties.Resources.binoculars_16, "Imports View Types from another open project");

            // 4. Create buttons
            PushButton myButton13 = panel000.AddItem(BtnWorkingViews.Data) as PushButton;
            PushButton myButton1 = panel000.AddItem(BtnForestGen.Data) as PushButton;
#if REVIT2022
            PushButton myButton3 = panel000.AddItem(BtnDuplicateSheet.Data) as PushButton;
#endif
            PushButton labelOffsetButton = panel000.AddItem(BtnLabelOffset.Data) as PushButton;
            PushButton myButton4 = panel000.AddItem(BtnFloorByDepartment.Data) as PushButton;
            PushButton myButton5 = panel000.AddItem(BtnLinesByRoom.Data) as PushButton;
            PushButton myButton9 = panel000.AddItem(BtnRenameView.Data) as PushButton;
            PushButton myButton11 = panel000.AddItem(BtnSetTypeImageCommand.Data) as PushButton;
            PushButton mybutton12 = panel600.AddItem(BtnStorefrontElevation.Data) as PushButton;
            PushButton myButton6 = panel800.AddItem(BtnRoomPlanGen.Data) as PushButton;
            PushButton myButton2 = panel900.AddItem(BtnFloorByRoom.Data) as PushButton;
            PushButton myButton10 = panel800.AddItem(BtnInteriorElevation.Data) as PushButton;
            PushButton myButton14 = panel800.AddItem(BtnDimensionOverride.Data) as PushButton;
            PushButton myButton15 = panel000.AddItem(BtnImportViewType.Data) as PushButton;
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
            splitButton2.AddPushButton(BtnDoorFireRating.Data);
            splitButton2.AddPushButton(BtnDoorCMUGWB.Data);
            splitButton2.AddPushButton(BtnDoorStorefrontMark.Data);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }


    }
}
