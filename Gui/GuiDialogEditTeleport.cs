using System;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TeleportationNetwork
{
    public class GuiDialogEditTeleport : GuiDialogGeneric
    {
        public override bool PrefersUngrabbedMouse => true;
        public override bool DisableMouseGrab => true;
        public override double DrawOrder => 0.2;

        private readonly TeleportManager _manager;
        private readonly BlockPos _pos;
        private readonly string[] _icons;

        private int[] _colors;
        private Teleport _teleport = null!;
        private TeleportClientData _data = new();

        public GuiDialogEditTeleport(ICoreClientAPI capi, BlockPos pos, TeleportMapLayer? layer = null)
            : base(Lang.Get($"{Constants.ModId}:edittpdlg-title"), capi)
        {
            _pos = pos;

            if (layer == null)
            {
                var mapManager = capi.ModLoader.GetModSystem<WorldMapManager>();
                layer = (TeleportMapLayer)mapManager.MapLayers.First(l => l is TeleportMapLayer);
            }

            _icons = layer?.WaypointIcons ?? [];
            _colors = layer?.WaypointColors.ToArray() ?? [];
            _manager = capi.ModLoader.GetModSystem<TeleportManager>();
        }

        public override bool TryOpen()
        {
            var otherDialog = capi.OpenedGuis.FirstOrDefault(gui =>
                gui is GuiDialogEditTeleport ||
                gui is GuiDialogEditWayPoint ||
                gui is GuiDialogAddWayPoint);
            (otherDialog as GuiDialog)?.TryClose();

            _teleport = _manager.Points[_pos]!;
            if (_teleport == null)
            {
                return false;
            }

            _data = _teleport.GetClientData(capi);

            ComposeDialog();
            return base.TryOpen();
        }

        private void ComposeDialog()
        {
            SingleComposer?.Dispose();

            bool mapAllowed = true;
            IAttribute value = capi.World.Config["allowMap"];
            if(value != null)
            {
                if (value.GetValue() is bool bValue)
                {
                    mapAllowed = bValue;
                }
                else if(value.GetValue() is string sValue)
                {
                    mapAllowed = sValue.ToLower().Equals("true");
                } else 
                {
                    mapAllowed = false;
                }
            }

            if (!_colors.Contains(TeleportClientData.DefaultColor))
            {
                _colors = _colors.Append(TeleportClientData.DefaultColor);
            }

            int iconIndex = _icons.IndexOf(_data.Icon);
            if (iconIndex < 0)
            {
                iconIndex = 0;
            }

            int colorIndex = _colors.IndexOf(_data.Color);
            if (colorIndex < 0)
            {
                _colors = _colors.Append(_data.Color);
                colorIndex = _colors.Length - 1;
            }

            int colorIconSize = 22;
            int spacing = -15;
            int rowHeight = 25;
            int colorRows = (int)Math.Ceiling((double)_colors.Length / 11);
            int iconRows = (int)Math.Ceiling((double)_icons.Length / 9);
            int noteMaxLines = 5;

            var leftColumn = ElementBounds.Fixed(0, 28, 100, rowHeight);
            var rightColumn = leftColumn.RightCopy().WithFixedWidth(300);

            var nameLabel = leftColumn.FlatCopy();
            var nameInput = rightColumn.FlatCopy();

            var noteLabel = leftColumn.FlatCopy().FixedUnder(nameLabel, spacing)
                .WithFixedHeight((GuiStyle.SmallFontSize + 6) * noteMaxLines);
            var noteArea = rightColumn.FlatCopy().FixedUnder(nameInput, spacing)
                .WithFixedHeight((GuiStyle.SmallFontSize + 6) * noteMaxLines);

            var orderLabel = leftColumn.FlatCopy().FixedUnder(noteLabel, spacing);
            var orderInput = rightColumn.FlatCopy().FixedUnder(noteArea, spacing);

            var pinnedLabel = leftColumn.FlatCopy().FixedUnder(orderLabel, spacing);
            var pinnedSwitch = rightColumn.FlatCopy().FixedUnder(orderInput, spacing);

            var globalLabel = leftColumn.FlatCopy().FixedUnder(pinnedLabel, spacing);
            var globalSwitch = rightColumn.FlatCopy().FixedUnder(pinnedSwitch, spacing);

            var colorLabel = leftColumn.FlatCopy().FixedUnder(globalLabel, spacing);
            var colorPicker = rightColumn.FlatCopy().FixedUnder(globalSwitch, spacing)
                .WithFixedSize(colorIconSize, colorIconSize);

            var iconLabel = leftColumn.FlatCopy()
                .FixedUnder(colorLabel, spacing + (colorIconSize + 5) * (colorRows - 1));
            var iconPicker = rightColumn.FlatCopy()
                .FixedUnder(colorPicker, spacing + (colorIconSize + 5) * (colorRows - 1))
                .WithFixedSize(colorIconSize + 5, colorIconSize + 5);

            var buttonRow = ElementBounds.Fixed(0, 28, 400, 25);
            var cancelButton = buttonRow.FlatCopy()
                .FixedUnder(iconLabel, spacing + (colorIconSize + 10) * (iconRows - 1))
                .WithFixedWidth(100);
            var saveButton = buttonRow.FlatCopy()
                .FixedUnder(iconPicker, spacing + (colorIconSize + 10) * (iconRows - 1))
                .WithFixedWidth(100)
                .WithAlignment(EnumDialogArea.RightFixed);

            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(leftColumn, rightColumn);

            var dialogBounds =
                ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            Boolean useFallback = false;
            if (mapAllowed) // Only allow editing of markers when world map is available as icons are not loaded without a map
            {
                try
                {
                    SingleComposer = capi.Gui
                        .CreateCompo($"{Constants.ModId}:edittpdlg", dialogBounds)
                        .AddShadedDialogBG(bgBounds, false)
                        .AddDialogTitleBar(DialogTitle, () => TryClose())
                        .BeginChildElements(bgBounds)
                            .AddStaticText(Lang.Get("Name"), CairoFont.WhiteSmallText(), nameLabel)
                            .AddTextInput(nameInput, OnNameChanged, CairoFont.TextInput(), "nameInput")

                            .AddStaticText(Lang.Get("Note"), CairoFont.WhiteSmallText(), noteLabel)
                            .AddTextArea(noteArea, OnNoteChanged, CairoFont.TextInput(), "noteInput")

                            .AddStaticText(Lang.Get($"{Constants.ModId}:edittpdlg-order-label"), CairoFont.WhiteSmallText(), orderLabel)
                            .AddNumberInput(orderInput, OnOrderChanged, CairoFont.WhiteSmallText(), "orderInput")

                            .AddStaticText(Lang.Get("Pinned"), CairoFont.WhiteSmallText(), pinnedLabel)
                            .AddSwitch(OnPinnedChanged, pinnedSwitch, "pinnedSwitch")

                            .AddStaticText(Lang.Get($"{Constants.ModId}:edittpdlg-global-label"), CairoFont.WhiteSmallText(), globalLabel)
                            .AddSwitch(null, globalSwitch, "globalSwitch")

                            .AddRichtext(Lang.Get("waypoint-color"), CairoFont.WhiteSmallText(), colorLabel)
                            .AddColorListPicker(_colors, OnColorSelected, colorPicker, 270, "colorPicker")

                            .AddStaticText(Lang.Get("Icon"), CairoFont.WhiteSmallText(), iconLabel)
                            .AddIconListPicker(_icons, OnIconSelected, iconPicker, 270, "iconPicker")

                            .AddSmallButton(Lang.Get("Cancel"), OnCancel, cancelButton, EnumButtonStyle.Normal)
                            .AddSmallButton(Lang.Get("Save"), OnSave, saveButton, EnumButtonStyle.Normal, key: "saveButton")
                        .EndChildElements()
                        .Compose();
                }
                catch (Exception e)
                {
                    useFallback = true; // Use fallback when icons are not loaded and there is no allowMap config that says so
                }
            }
            if(!mapAllowed || useFallback)
            {
                SingleComposer = capi.Gui
                    .CreateCompo($"{Constants.ModId}:edittpdlg", dialogBounds)
                    .AddShadedDialogBG(bgBounds, false)
                    .AddDialogTitleBar(DialogTitle, () => TryClose())
                    .BeginChildElements(bgBounds)
                        .AddStaticText(Lang.Get("Name"), CairoFont.WhiteSmallText(), nameLabel)
                        .AddTextInput(nameInput, OnNameChanged, CairoFont.TextInput(), "nameInput")

                        .AddStaticText(Lang.Get("Note"), CairoFont.WhiteSmallText(), noteLabel)
                        .AddTextArea(noteArea, OnNoteChanged, CairoFont.TextInput(), "noteInput")

                        .AddStaticText(Lang.Get($"{Constants.ModId}:edittpdlg-order-label"), CairoFont.WhiteSmallText(), orderLabel)
                        .AddNumberInput(orderInput, OnOrderChanged, CairoFont.WhiteSmallText(), "orderInput")

                        .AddStaticText(Lang.Get("Pinned"), CairoFont.WhiteSmallText(), pinnedLabel)
                        .AddSwitch(OnPinnedChanged, pinnedSwitch, "pinnedSwitch")

                        .AddStaticText(Lang.Get($"{Constants.ModId}:edittpdlg-global-label"), CairoFont.WhiteSmallText(), globalLabel)
                        .AddSwitch(null, globalSwitch, "globalSwitch")

                        .AddSmallButton(Lang.Get("Cancel"), OnCancel, cancelButton, EnumButtonStyle.Normal)
                        .AddSmallButton(Lang.Get("Save"), OnSave, saveButton, EnumButtonStyle.Normal, key: "saveButton")
                    .EndChildElements()
                    .Compose();
            }

            SingleComposer.ColorListPickerSetValue("colorPicker", colorIndex);
            SingleComposer.IconListPickerSetValue("iconPicker", iconIndex);

            SingleComposer.GetTextInput("nameInput").SetValue(_teleport.Name);
            SingleComposer.GetTextArea("noteInput").SetValue(_data.Note);
            SingleComposer.GetNumberInput("orderInput").SetValue(_data.SortOrder);
            SingleComposer.GetSwitch("pinnedSwitch").SetValue(_data.Pinned);

            var globalSwitchButton = SingleComposer.GetSwitch("globalSwitch");
            globalSwitchButton.SetValue(_teleport.IsGlobal);
            globalSwitchButton.Enabled = capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative;

            SingleComposer.GetTextArea("noteInput").SetMaxLines(noteMaxLines);
        }

        private void OnNameChanged(string name)
        {
            SingleComposer.GetButton("saveButton").Enabled = !string.IsNullOrWhiteSpace(name);
        }

        private void OnNoteChanged(string note)
        {
            _data.Note = note;
        }

        private void OnOrderChanged(string orderString)
        {
            if (int.TryParse(orderString, NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture, out int order))
            {
                _data.SortOrder = order;
            }
            else
            {
                SingleComposer.GetNumberInput("orderInput").SetValue(_data.SortOrder);
            }
        }

        private void OnPinnedChanged(bool pinned)
        {
            _data.Pinned = pinned;
        }

        private void OnIconSelected(int index)
        {
            _data.Icon = _icons[index];
        }

        private void OnColorSelected(int index)
        {
            _data.Color = _colors[index];
        }

        private bool OnCancel()
        {
            TryClose();
            return true;
        }

        private bool OnSave()
        {
            _teleport.Name = SingleComposer.GetTextInput("nameInput").GetText();
            _teleport.IsGlobal = SingleComposer.GetSwitch("globalSwitch").On;
            _teleport.SetClientData(capi, _data);

            capi.ModLoader.GetModSystem<TeleportManager>().UpdateTeleport(_teleport);

            TryClose();
            return true;
        }

        public override bool CaptureAllInputs()
        {
            return IsOpened();
        }
    }
}
