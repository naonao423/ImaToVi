﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using OpenCvSharp;
using MSAPI = Microsoft.WindowsAPICodePack;
using System.Reflection;
using System.ComponentModel;

namespace ImaToVi
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        Mat[][] MatDataS = new Mat[25][];
        int[] firstImSize = new int[2];
        int[] Matid = new int[25] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
                                    0, 0, 0, 0, 0 };
        int minCount = 1000000;

        public MainWindow()
        {
            InitializeComponent();
            SaveDirectory.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"/test.avi";
            Progress.Value = 0;
        }

        /// <summary>
        /// フォルダ内のファイルの詳細を調べるメソッド
        /// </summary>
        private void ShowInformation(string filePath,string sectionId)
        {
            //どの部分を受け取っているかの算出
            string sectionIdString = sectionId.Split(new string[] { "D" }, StringSplitOptions.None)[2];
            char[] sectionIdStrings = sectionIdString.ToCharArray();
            int sectionIdnumber = (int)Char.GetNumericValue(sectionIdStrings[0]) * 5 + (int)Char.GetNumericValue(sectionIdStrings[1]);
            string[] fileList = Directory.GetFiles(filePath, "*", SearchOption.TopDirectoryOnly);
            int fileCount = fileList.Length;
            var ext = System.IO.Path.GetExtension(fileList[0]).ToLower();
            MatDataS[sectionIdnumber] = new Mat[fileCount];
            int imWidth;
            int imHeight;
            int ident = 0;
            using (Mat firstPicture = new Mat(fileList[0]))
            {
               imWidth = firstPicture.Size().Width;
               imHeight = firstPicture.Size().Height;
            }
            var errorExt = 0;
            foreach (string file in fileList)
            {
                //画像のデータを保管
                MatDataS[sectionIdnumber][ident] = new Mat(file);
                if (ext != System.IO.Path.GetExtension(file).ToLower()){
                    errorExt = 1;
                }
                else
                {
                    ext = System.IO.Path.GetExtension(file).ToLower();
                }
                ident += 1;
            }
            Matid[sectionIdnumber] = 1;
            var NameDy = (TextBlock)this.FindName("Name"+sectionIdString);
            var CountDy = (TextBlock)this.FindName("Count" +sectionIdString);
            var TypeDy = (TextBlock)this.FindName("Type"+sectionIdString);
            var SizeDy = (TextBlock)this.FindName("Size"+sectionIdString);
            NameDy.Text = System.IO.Path.GetFileName(filePath);
            CountDy.Text = fileCount.ToString();
            TypeDy.Text = (ext);
            SizeDy.Text = imWidth.ToString() + "x" + imHeight.ToString();
            if ( minCount > fileCount)
            {
                minCount = fileCount;
            }
            if (sectionIdnumber == 0)
            {
                firstImSize[0] = imWidth;
                firstImSize[1] = imHeight;
                var sightDy = (Border)this.FindName("sight" + sectionIdString);
                sightDy.Background = new SolidColorBrush(Colors.Green);
            }
            else
            {
                if (firstImSize[0] != imWidth || firstImSize[1] != imHeight)
                {
                    var sightDy = (Border)this.FindName("sight" + sectionIdString);
                    sightDy.Background = new SolidColorBrush(Colors.Yellow);
                }
                else
                {
                    var sightDy = (Border)this.FindName("sight" + sectionIdString);
                    sightDy.Background = new SolidColorBrush(Colors.Green);
                }
            if (errorExt == 1)
                {
                    var sightDy = (Border)this.FindName("sight" + sectionIdString);
                    sightDy.Background = new SolidColorBrush(Colors.Red);
                }
            }
        }


        /// <summary>
        /// D＆Dを確立するメソッド
        /// </summary>
        private void Grid_Drop(object sender, DragEventArgs e)
        {
            string sectionId = (((Grid)sender).Name);
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null)
            {
                return;
            }
            ShowInformation(files[0], sectionId);
        }
        private void Grid_PreviewDragOver(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            //ドロップすることができるのはフォルダかつ一つのファイルにしている
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true) &&
                files.Length == 1 &&
                File.GetAttributes(files[0]).HasFlag(FileAttributes.Directory))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }


        /// <summary>
        /// 保存先を指定するメソッド
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectSaveFolder(object sender, RoutedEventArgs e)
        {
            var dlg = new MSAPI::Dialogs.CommonOpenFileDialog();
            // フォルダ選択ダイアログ（falseにするとファイル選択ダイアログ）
            dlg.IsFolderPicker = true;
            // タイトル
            dlg.Title = "フォルダを選択してください";
            // 初期ディレクトリ
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (dlg.ShowDialog() == MSAPI::Dialogs.CommonFileDialogResult.Ok)
            {
                SaveDirectory.Text = dlg.FileName;
            }
        }

        private void eClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// 動画作成実行
        /// </summary>
        private void StartConvert(object sender, RoutedEventArgs e)
        {
            if (MatDataS[0] == null)
            {
                return;
            }

            Progress.Value = 0;
            //まずインプットされているエリアの取得からする
            var maxColumn = 0;
            var maxRow = 0;
            var ident = 0;
            foreach(int num in Matid)
            {

                if (num == 1){
                    var Row = ident / 5;
                    var Column = ident % 5;
                    if (Row > maxRow)
                    {
                        maxRow = Row;
                    }
                    if ( Column > maxColumn)
                    {
                        maxColumn = Column;
                    }
                }
                ++ident;
            }
            var paletteImWidth = firstImSize[0] * (maxColumn + 1);
            var paletteImHeight = firstImSize[1] * (maxRow + 1);
            Mat src = OpenCvSharp.Extensions.BitmapConverter.ToMat(Properties.Resources.Black);

            //根底の画像を設定
            Mat paletteIm = new Mat();
            Cv2.Resize(src,paletteIm,new OpenCvSharp.Size(paletteImWidth,paletteImHeight),0,0,InterpolationFlags.Cubic);
           
            //動画の作成
            VideoWriter video = new VideoWriter(SaveDirectory.Text, FourCC.MJPG, int.Parse(fps.Text), new OpenCvSharp.Size(paletteImWidth,paletteImHeight));
            int indexNum;
            Console.WriteLine(minCount);
            for (var i = 0; i< minCount; i++)
            {
                    Progress.Value += 100/minCount;
                var paletteImTemp = paletteIm;
                for (var row = 0; row <= maxRow; row++)
                {
                    for (var col = 0; col <= maxColumn; col++)
                    {
                        indexNum = row * 5 + col;
                        if (MatDataS[indexNum][i] != null)
                        {
                            //貼り付け範囲の指定
                            var rect = new OpenCvSharp.Rect(firstImSize[0] * col, firstImSize[1] * row, firstImSize[0], firstImSize[1]);
                            paletteImTemp[rect] = MatDataS[indexNum][i];
                        }
                    }
                }
                video.Write(paletteImTemp);
            }
            video.Dispose();

            //foreach (Mat img in MatDataS[0])
            //{
            //    video.Write(img);
            //}

        }






        /// <summary>
        /// ラジオボタンの設定
        /// </summary>
        private void None_Checked(object sender, RoutedEventArgs e)
        {
            radDeg.Text = "0";
            radMsec.Text = "0";
            radDeg.IsEnabled = false;
            radMsec.IsEnabled = false;
        }
        private void Deg_Checked(object sender, RoutedEventArgs e)
        {
            radMsec.Text = "0";
            radDeg.IsEnabled = true;
            radMsec.IsEnabled = false;
        }
        private void Msec_Checked(object sender, RoutedEventArgs e)
        {
            radDeg.Text = "0";
            radDeg.IsEnabled = false;
            radMsec.IsEnabled = true;

        }

        private void help(object sender, RoutedEventArgs e)
        {
            var window = new help();
            bool? res = window.ShowDialog();
        }
    }
}
