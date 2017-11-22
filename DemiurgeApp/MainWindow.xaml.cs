using DemiurgeLib;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using static DemiurgeLib.Common.Utils;

namespace DemiurgeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SetUpArgsRows(new WaterTableArgs(), this.WaterTableArgsGrid);
            SetUpArgsRows(new MeterScaleMap.Args(null, null, null, null), this.MeterScaleMapArgsGrid);

            // TODO: Enable interactivity, so you can generate a WTF, then generate an MSM, then generate output, all independently.
        }

        private void SetUpArgsRows<T>(T prototype, Grid target)
        {
            var fields = typeof(T).GetFields();

            for (int idx = 0; idx < fields.Length; idx++)
            {
                target.RowDefinitions.Add(new RowDefinition());

                // Make the label.
                TextBlock label = new TextBlock();
                label.Text = fields[idx].Name;
                Grid.SetRow(label, idx);
                Grid.SetColumn(label, 0);
                target.Children.Add(label);

                // Make the content display/setter.
                Type type = fields[idx].FieldType;
                if (type == typeof(int) || type == typeof(long) || type == typeof(float))
                {
                    TextBox value = new TextBox();
                    value.Text = fields[idx].GetValue(prototype).ToString();
                    Grid.SetRow(value, idx);
                    Grid.SetColumn(value, 2);
                    target.Children.Add(value);
                }
                else if (type == typeof(string))
                {
                    CheckBox checkBox = new CheckBox();
                    Grid.SetRow(checkBox, idx);
                    Grid.SetColumn(checkBox, 1);
                    target.Children.Add(checkBox);

                    // TODO: Support string fields other than files.
                    Button button = new Button();
                    button.Content = fields[idx].GetValue(prototype);
                    button.Click += (sender, e) =>
                    {
                        // File picker.
                        //var dialog = new System.Windows.Forms.OpenFileDialog();
                        var dialog = new System.Windows.Forms.FolderBrowserDialog();
                        var result = dialog.ShowDialog();
                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            // Do something.
                            //button.Content = dialog.FileName;
                            button.Content = dialog.SelectedPath;
                        }
                    };
                    Grid.SetRow(button, idx);
                    Grid.SetColumn(button, 2);
                    target.Children.Add(button);
                }
            }
        }

        private void RunMeterScaleMapScenario(object sender, RoutedEventArgs e)
        {
            var wta = new WaterTableArgs();
            wta.inputPath = "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\input\\";
            var waters = new FieldFromBitmap(new Bitmap(wta.inputPath + "coastline.png"));
            var heights = new FieldFromBitmap(new Bitmap(wta.inputPath + "heights.png"));
            var roughness = new FieldFromBitmap(new Bitmap(wta.inputPath + "roughness.png"));
            var msmArgs = new MeterScaleMap.Args(waters, heights, roughness, null);
            msmArgs.seed = System.DateTime.UtcNow.Ticks;
            msmArgs.metersPerPixel = 800;
            msmArgs.riverCapacityToMetersWideFunc = c => (float)Math.Pow(msmArgs.metersPerPixel * SplineTree.CAPACITY_DIVISOR * c, 0.5f) / 4f;
            msmArgs.baseHeightMaxInMeters = 500;
            msmArgs.valleyStrength = 0.98f;
            var msm = new MeterScaleMap(msmArgs);
            
            msm.OutputHighLevelMaps(new Bitmap(waters.Width, waters.Height), "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\");
            msm.OutputMapGrid(100, "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\", "submap", 32);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
