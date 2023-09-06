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
            ButtonDataClass myButtonData1 = new ButtonDataClass("ForestGen", "Forest \rGenerator", ForestGen.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Draw detail lines into a polygonal shape for your trees to be placed inside. Follow the command prompts to define amount and type.");
            ButtonDataClass myButtonData2 = new ButtonDataClass("FloorByRoom", "Floors by \rRooms", FloorByRoom.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select rooms. Run command and follow prompt to select type of floor to populate.");
#if REVIT2022
            ButtonDataClass myButtonData3 = new ButtonDataClass("DuplicateSheet", "Duplicate \rSheet", DuplicateViewsCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select Sheet. Run command.");
#endif
            ButtonDataClass myButtonData4 = new ButtonDataClass("FloorByDepartment", "Floors by \rDepartment", FloorByDepartment.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select rooms. Run command. Get Floors.");
            ButtonDataClass myButtonData5 = new ButtonDataClass("LinesByRoom", "Lines by \rRoom", LinesByRoom.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select rooms. Run command. Get Outline.");
            ButtonDataClass myButtonData6 = new ButtonDataClass("RoomPlanGen", "Plans by \rRoom", RoomPlanGen.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select rooms. Run command. Get Plans.");
            ButtonDataClass myButtonData7 = new ButtonDataClass("CenterRoom", "Center\rRoom", RoomTagCenteringCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select rooms. Center rooms.");
            ButtonDataClass myButtonData8 = new ButtonDataClass("CenterRoomTag", "Center\rRoom tag", RoomTagCenteringCommandTag.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select rooms. Center tags.");
            ButtonDataClass myButtonData9 = new ButtonDataClass("RenameView", "Rename\rView", RenameView.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select room. Rename View.");
            ButtonDataClass myButtonData10 = new ButtonDataClass("DoorFireRating", "Door Fire\rRating", DoorFireRating.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select doors to set their fire rating.");
            ButtonDataClass myButtonData11 = new ButtonDataClass("DoorCMU/GWB", "Door Inset\rOr Wrapped", DoorInsetWrap.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select doors to set whether they are inset or wrapped.");
            ButtonDataClass myButtonData12 = new ButtonDataClass("DoorStorefrontMark", "Door Storefront\rMark", DoorStorefrontMark.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select storefront or curtainwall doors. Get mark based on the storefront mark.");
            ButtonDataClass myButtonData13 = new ButtonDataClass("StorefrontElevation", "Make Storefront\rElevations", CurtainWallElevationAddIn.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select storefront or curtainwall to create elevations of them.");
            ButtonDataClass myButtonData14 = new ButtonDataClass("InteriorElevation", "Make Interior\rElevations", InteriorElevation.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select rooms to elevate.");
            ButtonDataClass myButtonData15 = new ButtonDataClass("FamilySizes", "Get family\rsizes in model", FamilyFileSizeReporter.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Get sizes of all families in model.");
            ButtonDataClass myButtonData16 = new ButtonDataClass("ModifyCropBoundaryCommand", "Raise Crop\rBoundary", ModifyCropBoundaryCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Raises the crop boundary of selected views");
            ButtonDataClass myButtonData17 = new ButtonDataClass("FilledRegionDonut", "Create\rDonut", CreateFilledRegionCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select views. Get Donuts.");
            ButtonDataClass myButtonData18 = new ButtonDataClass("SetTypeImageCommand", "Set new\rType Image", SetTypeImageCommand.GetMethod(), EfficiencyPack_Resources.Properties.Resources.Blue_32, EfficiencyPack_Resources.Properties.Resources.Blue_16, "Select Family. Input Type Image.");

            // 4. Create buttons
            PushButton myButton1 = panel000.AddItem(myButtonData1.Data) as PushButton;
#if REVIT2022
            PushButton myButton3 = panel000.AddItem(myButtonData3.Data) as PushButton;
#endif
            PushButton myButton4 = panel000.AddItem(myButtonData4.Data) as PushButton;
            PushButton myButton5 = panel000.AddItem(myButtonData5.Data) as PushButton;
            PushButton myButton9 = panel000.AddItem(myButtonData9.Data) as PushButton;
            PushButton myButton11 = panel000.AddItem(myButtonData18.Data) as PushButton;
            PushButton mybutton12 = panel600.AddItem(myButtonData13.Data) as PushButton;
            PushButton myButton6 = panel800.AddItem(myButtonData6.Data) as PushButton;
            PushButton myButton2 = panel900.AddItem(myButtonData2.Data) as PushButton;
            PushButton myButton10 = panel800.AddItem(myButtonData14.Data) as PushButton;
            //5. Split buttons
            SplitButtonData splitButtonData = new SplitButtonData("Center Room", "Center\rRoom");
            SplitButton splitButton = panel000.AddItem(splitButtonData) as SplitButton;
            splitButton.AddPushButton(myButtonData7.Data);
            splitButton.AddPushButton(myButtonData8.Data);
            SplitButtonData splitButtonData3 = new SplitButtonData("View Modifiers", "View\rModifier");
            SplitButton splitButton3 = panel800.AddItem(splitButtonData3) as SplitButton;
            splitButton3.AddPushButton(myButtonData16.Data);
            splitButton3.AddPushButton(myButtonData17.Data);
            SplitButtonData splitButtonData2 = new SplitButtonData("Door Modifiers", "Door\rModifier");
            SplitButton splitButton2 = panel600.AddItem(splitButtonData2) as SplitButton;
            splitButton2.AddPushButton(myButtonData10.Data);
            splitButton2.AddPushButton(myButtonData11.Data);
            splitButton2.AddPushButton(myButtonData12.Data);

            //PushButton myButton11 = panel000.AddItem(myButtonData15.Data) as PushButton;

            // NOTE:
            // To create a new tool, copy lines 35 and 39 and rename the variables to "myButtonData3" and "myButton3". 
            // Change the name of the tool in the arguments of line 

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }


    }
}
