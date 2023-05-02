using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using MahApps.Metro.IconPacks;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using WindowsInput.Native;


namespace GenerateSymbols
{
    class Program
    {
        static string images_dir = "symbols";
        static int image_width = 64;

        static int md_icon_height = 50;
        static int md_table_cols = 3;

        private static void EnsureExists(string dir_name)
        {
            if (!Directory.Exists(dir_name))
            {
                Directory.CreateDirectory(dir_name);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            EnsureExists("wiki");
            EnsureExists("symbols");

            SaveOptikeySymbols();
            SaveMaterialDesignSymbols();
            SaveRPGAwesomeSymbols();
            SaveActions();
            SavePressableKeys();

#if DEBUG
            Console.ReadLine();
#endif
        }

        static void SaveOptikeySymbols()
        {
            // Export all built-in Optikey symbols which are defined in "KeySymbols.xaml"
            // - Save as PNG
            // - Write markdown wiki page with names and symbols in a table

            string md_fname = "Optikey-symbols.md";
            using (StreamWriter md_file = new StreamWriter(md_fname))
            {
                md_file.WriteLine(GetMarkdownPreamble());

                string xaml = File.ReadAllText("KeySymbols.xaml");
                ResourceDictionary dict = (ResourceDictionary)XamlReader.Parse(xaml);

                int count = 0;
                foreach (object key in dict.Keys)
                {
                    Console.WriteLine(key);

                    if (dict[key] is Geometry geometry)
                    {
                        string fileName = $"{images_dir}/{key}.png";

                        SaveGeometry(geometry, fileName, image_width);

                        // save the bitmap and label to markdown table
                        if (count % md_table_cols == 0)
                        {
                            if (count > 0)
                                md_file.WriteLine("</tr>");
                            md_file.WriteLine("<tr>");
                        }
                        md_file.WriteLine(GetTableCell(fileName, key.ToString(), md_icon_height));

                        count++;
                    }
                }
                md_file.WriteLine("</tr>");
                md_file.WriteLine(GetMarkdownPostamble());
            }
        }


        static void SaveActions()
        {
            string md_fname = "Optikey-actions.md";
            using (StreamWriter md_file = new StreamWriter(md_fname))
            {
                md_file.WriteLine("## Optikey actions (aka FunctionKeys)");
                md_file.WriteLine();
                md_file.WriteLine("This is a list of all built in Optikey actions, that can be used in an `<Action>` element");
                md_file.WriteLine();

                foreach (FunctionKeys key in Enum.GetValues(typeof(FunctionKeys)))
                {
                    VirtualKeyCode? keyCode =  key.ToVirtualKeyCode();
                    if (keyCode.HasValue)
                    {
                        md_file.WriteLine($"- { key } (equivalent to `<KeyPress>{key}</KeyPress>`)");
                    }
                    else
                    {
                        md_file.WriteLine($"- { key }");
                    }
                }
            }
        }

        static void SavePressableKeys()
        {
            string md_fname = "Optikey-keys.md";
            using (StreamWriter md_file = new StreamWriter(md_fname))
            {
                md_file.WriteLine("## Optikey actions (aka FunctionKeys)");
                md_file.WriteLine();
                md_file.WriteLine("This is a list of all key names defined by Optikey, that can be used in an `<KeyPress>` element or similar");
                md_file.WriteLine();

                foreach (FunctionKeys key in Enum.GetValues(typeof(FunctionKeys)))
                {
                    VirtualKeyCode? keyCode = key.ToVirtualKeyCode();
                    if (keyCode.HasValue)
                    {
                        md_file.WriteLine($"- { key }");
                    }
                }
            }
        }

        static void SaveMaterialDesignSymbols()
        {
            // Save all the icons that come from mahApps Material Design icon library
            string md_fname = "MaterialDesign-symbols.md";
            using (StreamWriter md_file = new StreamWriter(md_fname))
            {
                md_file.WriteLine(GetMarkdownPreamble());
                int count = 0;

                foreach (PackIconMaterialDesignKind key in Enum.GetValues(typeof(PackIconMaterialDesignKind)))
                {
                    if (key == PackIconMaterialDesignKind.None)
                        continue;

                    Console.WriteLine(key);

                    // Extract geometry from icon
                    var icon = new PackIconMaterialDesign();
                    icon.Kind = key;
                    Geometry geometry = Geometry.Parse(icon.Data);

                    // Flip icon (up <-> down) to match coordinate system                        
                    ScaleTransform transform = new ScaleTransform(1, -1, 0, geometry.Bounds.Top + geometry.Bounds.Height / 2);
                    PathGeometry geometryTransformed = Geometry.Combine(Geometry.Empty, geometry, GeometryCombineMode.Union, transform);

                    string fileName = $"{images_dir}/MaterialDesign/{key}.png";
                    SaveGeometry(geometryTransformed, fileName, image_width);

                    // save the bitmap and label to markdown table
                    if (count % md_table_cols == 0)
                    {
                        if (count > 0)
                            md_file.WriteLine("</tr>");
                        md_file.WriteLine("<tr>");
                    }

                    md_file.WriteLine(GetTableCell(fileName, key.ToString(), md_icon_height));

                    count++;

                }
                md_file.WriteLine("</tr>");
                md_file.WriteLine(GetMarkdownPostamble());
            }
        }

        static void SaveRPGAwesomeSymbols()
        {
            // Save all the icons that come from RPG Awesome icon library
            string md_fname = "RGP-Awesome-symbols.md";
            using (StreamWriter md_file = new StreamWriter(md_fname))
            {
                md_file.WriteLine(GetMarkdownPreamble());
                int count = 0;

                foreach (PackIconRPGAwesomeKind key in Enum.GetValues(typeof(PackIconRPGAwesomeKind)))
                {
                    if (key == PackIconRPGAwesomeKind.None)
                        continue;

                    Console.WriteLine(key);

                    // Extract geometry from icon
                    var icon = new PackIconRPGAwesome();
                    icon.Kind = key;
                    Geometry geometry = Geometry.Parse(icon.Data);

                    // Flip icon (up <-> down) to match coordinate system                        
                    ScaleTransform transform = new ScaleTransform(1, -1, 0, geometry.Bounds.Top + geometry.Bounds.Height / 2);
                    PathGeometry geometryTransformed = Geometry.Combine(Geometry.Empty, geometry, GeometryCombineMode.Union, transform);

                    string fileName = $"{images_dir}/RPGAwesome/{key}.png";
                    SaveGeometry(geometryTransformed, fileName, image_width);

                    // save the bitmap and label to markdown table
                    if (count % md_table_cols == 0)
                    {
                        if (count > 0)
                            md_file.WriteLine("</tr>");
                        md_file.WriteLine("<tr>");
                    }

                    md_file.WriteLine(GetTableCell(fileName, key.ToString(), md_icon_height));

                    count++;

                }
                md_file.WriteLine("</tr>");
                md_file.WriteLine(GetMarkdownPostamble());
            }
        }

        static string GetMarkdownPreamble(string caption = "Automatically generated list of symbols available")
        {
            string s = "<table cellspacing = \"0\" cellpadding = \"0\" >";
            s += $"<caption>{ caption }</caption>";
            s += "<tbody>";
            return s;
        }

        static string GetMarkdownPostamble()
        {
            return "</table>";
        }

        static string GetTableCell(string filename, string label, int height)
        {
            return $"<td>{ label }<br/><img src=\"{ filename }\" alt=\"{ label }\" height=\"{ height }\"></td>";
        }

        static void SaveGeometry(Geometry geometry, string filename, int new_width)
        {
            FileInfo f = new FileInfo(filename);
            EnsureExists(f.DirectoryName);        

            var bounds = geometry.Bounds;

            int orig_width = (int)(bounds.Left * 2 + bounds.Width);

            double scale = (double)new_width / (double)orig_width;
            ScaleTransform transform = new ScaleTransform(scale, scale);
            geometry = Geometry.Combine(Geometry.Empty, geometry, GeometryCombineMode.Union, transform);

            // get new dimensions
            bounds = geometry.Bounds;
            int width = (int)(bounds.Left * 2 + bounds.Width);
            int height = (int)(bounds.Top * 2 + bounds.Height);

            RenderTargetBitmap bmp = new RenderTargetBitmap(width, height, // Size
                                                                96, 96, // DPI 
                                                                PixelFormats.Default);

            // some code from https://stackoverflow.com/questions/9080231/how-to-save-geometry-as-image

            // The light-weight visual element that will draw the geometries
            DrawingVisual viz = new DrawingVisual();
            using (DrawingContext dc = viz.RenderOpen())
            {
                var rect = new Rect(0, 0, width, height);
                dc.DrawGeometry(System.Windows.Media.Brushes.White, null, new System.Windows.Media.RectangleGeometry(rect));
                dc.DrawGeometry(System.Windows.Media.Brushes.Black, null, geometry);
            }

            // draw the visual on the bitmap
            bmp.Render(viz);

            // instantiate an encoder to save the file
            PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
            // add this bitmap to the encoders set of frames
            pngEncoder.Frames.Add(BitmapFrame.Create(bmp));

            // save the bitmap as an .png file
            using (FileStream file = new FileStream(filename, FileMode.Create))
                pngEncoder.Save(file);
        }

    }
}
