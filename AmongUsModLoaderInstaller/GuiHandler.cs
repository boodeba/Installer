using System;
using System.IO;
using Gtk;

namespace AmongUsModLoaderInstaller
{
    internal static class GuiHandler
    {
        internal static void RunGui(bool isLinux)
        {
            var stream = typeof(Program).Assembly.GetManifestResourceStream("AmongUsModLoaderInstaller.Window.glade");
            if (stream == null) return;
            using var reader = new StreamReader(stream);
            Application.Init();
            var builder = new Builder();
            builder.AddFromString(reader.ReadToEnd());
            var window = Get<ApplicationWindow>("main_container");
            var typeSelector = Get<ComboBox>("type_selector");
            var path = Get<FileChooserButton>("path");
            var pathText = Get<Label>("path_label");
            var prefixPath = Get<FileChooserButton>("wine_prefix");
            var prefixText1 = Get<Label>("wine_prefix_label1");
            var prefixText2 = Get<Label>("wine_prefix_label2");
            var installButton = Get<Button>("install_button");
            var clientPathText = pathText.Text;
            var winePrefix1 = prefixText1.Text;
            var winePrefix2 = prefixText2.Text;
            var server = false;

            T Get<T>(string name) where T : Widget => (T) builder.GetObject(name);

            void SetText(bool steam)
            {
                if (steam)
                {
                    prefixText1.Text = "";
                    prefixText2.Text = "Steam Directory";
                }
                else
                {
                    prefixText1.Text = winePrefix1;
                    prefixText2.Text = winePrefix2;
                }
            }

            const string relativeGameSteamLocation = "steamapps/common/Among Us/";
            var steamCheck = Get<CheckButton>("steam_check");
            steamCheck.Active = true;

            ToggleHandler(window, EventArgs.Empty);
            steamCheck.Toggled += ToggleHandler;
            prefixPath.CurrentFolderChanged += (sender, args) =>
            {
                if (steamCheck.Active)
                {
                    path.SetCurrentFolder(prefixPath.CurrentFolder + "/" + relativeGameSteamLocation);
                }
            };

            typeSelector.Changed += (sender, args) =>
            {
                if (!typeSelector.GetActiveIter(out var iter)) return;
                var value = (string) typeSelector.Model.GetValue(iter, 0);
                if (value == "Server")
                {
                    server = true;
                    pathText.Text = "Installation Directory";
                    prefixPath.Hide();
                    prefixText1.Hide();
                    prefixText2.Hide();
                    steamCheck.Hide();
                }
                else
                {
                    server = false;
                    pathText.Text = clientPathText;
                    steamCheck.Show();
                    if (!steamCheck.Active && !isLinux) return;
                    prefixPath.Show();
                    prefixText1.Show();
                    prefixText2.Show();
                }
            };

            void ToggleHandler(object? sender, EventArgs args)
            {
                if (steamCheck.Active)
                {
                    var steam = isLinux
                        ? Environment.GetEnvironmentVariable("HOME") + "/.local/share/Steam/"
                        : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "/Steam/";
                    path.SetCurrentFolder(steam + relativeGameSteamLocation);

                    if (isLinux)
                    {
                        prefixText1.Show();
                        prefixText2.Show();
                        SetText(true);
                    }
                    else
                    {
                        prefixText1.Hide();
                        prefixText2.Hide();
                        prefixPath.Hide();
                    }

                    prefixPath.SetCurrentFolder(steam);
                }
                else
                {
                    path.UnselectAll();
                    path.Show();
                    pathText.Show();

                    if (isLinux)
                    {
                        SetText(false);
                        prefixText1.Show();
                        prefixText2.Show();
                        prefixPath.Show();
                        prefixPath.SetCurrentFolder(Environment.GetEnvironmentVariable("HOME") + "/.wine/");
                    }
                    else
                    {
                        prefixText1.Hide();
                        prefixText2.Hide();
                        prefixPath.Hide();
                    }
                }
            }

            installButton.Clicked += async (sender, args) =>
            {
                try
                {
                    await Installer.Run(isLinux, server, steamCheck.Active, true, path.CurrentFolder, prefixPath.CurrentFolder);
                }
                catch (Exception e)
                {
                    using var dialog = new MessageDialog(window, DialogFlags.Modal, MessageType.Error,
                        ButtonsType.Close, false, "{0}: {1}\n{2}", e, e.Message, e.StackTrace);
                    dialog.Run();
                    throw;
                }
            };
            window.DeleteEvent += (sender, args) => Application.Quit();
            window.ShowAll();
            Application.Run();
        }
    }
}