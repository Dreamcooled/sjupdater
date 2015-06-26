using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RestSharp.Extensions;
using SjUpdater.Model;
using SjUpdater.Utils;
using SjUpdater.ViewModel;

namespace SjUpdater
{
    /// <summary>
    /// Interaction logic for MultiDownloadSelectionGrid.xaml
    /// </summary>
    public partial class MultiDownloadSelectionGrid : UserControl
    {
        public MultiDownloadSelectionGrid()
        {
            InitializeComponent();
            ToggleRowCommand= new SimpleCommand<object,Row>(ToggleRow);
        }

        public static readonly DependencyProperty EpisodesProperty = DependencyProperty.Register(
            "Episodes", typeof (List<FavEpisodeData>), typeof (MultiDownloadSelectionGrid), new PropertyMetadata(null,new PropertyChangedCallback(EpisodesChanged)));

        private static void EpisodesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ((MultiDownloadSelectionGrid) dependencyObject).UpdateAll();
        }

        public List<FavEpisodeData> Episodes
        {
            get { return (List<FavEpisodeData>) GetValue(EpisodesProperty); }
            set { SetValue(EpisodesProperty, value); }
        }

        private static readonly Comparer<UploadData> UploadComparer = Comparer<UploadData>.Create(
            delegate(UploadData a, UploadData b)
            {
                if (a.Favorized == b.Favorized) return 0;
                if (a.Favorized) return -1;
                return 1;
            });
        private static readonly Comparer<FavEpisodeData> EpisodeComparer = Comparer<FavEpisodeData>.Create(
            delegate (FavEpisodeData a, FavEpisodeData b)
            {
                if (a.Season.Number == b.Season.Number && a.Number == b.Number) return 0;
                int r = 0;
                if (a.Season.Number > b.Season.Number) r= 1;
                if (a.Season.Number < b.Season.Number) r= -1;
                if (a.Season.Number == b.Season.Number)
                {
                    if (a.Number > b.Number) r= 1;
                    if (a.Number < b.Number) r=-1;
                    if (a.Number == b.Number) return 0;
                    return (Settings.Instance.SortEpisodesDesc) ? -r : r;
                }
                return (Settings.Instance.SortSeasonsDesc) ? -r : r;
            });


        public ICommand ToggleRowCommand { get; private set; }

        private class Header
        {
            public String Title { get; set; }
            public List<String> Hosters { get; set; }
            public ICommand ToggleCommand { get; set; }
            public ICommand ToggleAllCommand { get; set; }
        }

        private class Row
        {
            public String Title { get; set; }
            public String Tooltip { get; set; }
            public List<Cell> Cells { get; set; } 
        }


        private class Cell
        {
            public List<CellEntry> Entries { get; set; }
            public FavEpisodeData Episode { get; set; }
            public Header Header { get; set; }
        }

        private class CellEntry : PropertyChangedImpl
        {
            
            public Visibility Visibility { get; set; }
            public bool Enabled { get; set; }

            private bool _checked;
            public bool Checked
            {
                get { return _checked; }
                set
                {
                    _checked = value;
                    OnPropertyChanged();
                }
            }

            public String Link { get; set; }
        }

        private String BuildUploadTitle(UploadData upload)
        {
            return upload.Format + ", " + upload.Uploader + "\n" + upload.Size + ", " + upload.Runtime + ", " + upload.Language;
        }


        private List<Cell> _cells;
        private void UpdateAll()
        {
            dgrid.Columns.Clear();
            if (!Episodes.Any()) return;


            var episodesSorted = Episodes.OrderBy(e => e, EpisodeComparer); //sort Episodes by season, episode
            var uploadsSorted = episodesSorted.SelectMany(e => e.Downloads.Select(d => d.Upload)).OrderBy(u=>u,UploadComparer).Distinct(); //get a unique collection of the Uploads, sorted by fav/nofav
     
            List<Header> headers = new List<Header>();
            _cells = new List<Cell>();
            int i = 0;

            //Idea: In the following Loop we create 1 Header instance for ALL Uploads (regardless of the season) which have the same String-Representation

            foreach (var upload in uploadsSorted)
            {
                String title = BuildUploadTitle(upload);
                var existingHeader =headers.FirstOrDefault(h => h.Title == title);

                if (existingHeader == null)
                {
                    Header newHeader = new Header();
                    newHeader.Title = BuildUploadTitle(upload);
                    newHeader.ToggleCommand = new SimpleCommand<object,string>( s => ToggleColumn(newHeader,s));
                    newHeader.ToggleAllCommand = new SimpleCommand<object, object>(o => ToggleFullColumn(newHeader));
                    newHeader.Hosters =
                        episodesSorted.SelectMany(e => e.Downloads)
                            .Where(d => d.Upload == upload)
                            .SelectMany(d => d.Links.Keys)
                            .Distinct().ToList();
                    headers.Add(newHeader);

                    DataGridTemplateColumn column = new DataGridTemplateColumn();
                    column.Header = newHeader;
                    column.HeaderTemplate = new DataTemplate();
                    column.HeaderTemplate.VisualTree = new FrameworkElementFactory(typeof (MultiDownloadSelectionHeader));
                    column.HeaderStyle = (Style) FindResource("CenterGridHeaderStyle");
                    column.CellTemplate = new DataTemplate();
                    column.CellTemplate.VisualTree = new FrameworkElementFactory(typeof (MultiDownloadSelectionCell));
                    column.CellTemplate.VisualTree.SetBinding(DataContextProperty, new Binding("Cells[" + i + "]"));

                    dgrid.Columns.Add(column);

                    i++;

                }
                else //there's already an upload existing (maybe in another Season) with the same string represenation
                {
                    existingHeader.Hosters = episodesSorted.SelectMany(e => e.Downloads)
                           .Where(d => d.Upload == upload)
                           .SelectMany(d => d.Links.Keys).Union(existingHeader.Hosters)
                           .Distinct().ToList();
                }
            }

            DataGridTemplateColumn fColumn = new DataGridTemplateColumn();
            fColumn.HeaderStyle = (Style)FindResource("RemovedHeaderContentStyle");
            fColumn.Width = new DataGridLength(1,DataGridLengthUnitType.Star);
            dgrid.Columns.Add(fColumn);

            List<Row> rows = new List<Row>();
            foreach (var episode in episodesSorted)
            {
                Row r = new Row();
                r.Title = "S" + episode.Season.Number + " E" + episode.Number;
                if (episode.EpisodeInformation != null && !String.IsNullOrWhiteSpace(episode.EpisodeInformation.Title))
                {
                    r.Tooltip = episode.EpisodeInformation.Title;
                }
                r.Cells = new List<Cell>();

                bool firstSelected = true;

                foreach (var header in headers)
                {
                    var c = new Cell();
                    c.Header = header;
                    c.Entries = new List<CellEntry>();
                    c.Episode = episode;

                    DownloadData dloads = episode.Downloads.FirstOrDefault(da => BuildUploadTitle(da.Upload) == header.Title);
                    bool selected = dloads!=null && dloads.Upload.Favorized;
                    if (firstSelected && selected)
                    {
                        firstSelected = false;
                    } else if (!firstSelected)
                    {
                        selected = false;
                    }


                    foreach (var hoster in header.Hosters)
                    {
                        if (dloads!=null && dloads.Links.ContainsKey(hoster))
                        {
                            c.Entries.Add(new CellEntry {Visibility = Visibility.Visible,Enabled=true, Link = dloads.Links[hoster], Checked = selected});
                        }
                        else
                        {
                            c.Entries.Add(new CellEntry {Visibility = Visibility.Hidden,Enabled=false,Link = "",Checked = false});
                        }
                    }
                    _cells.Add(c);
                    r.Cells.Add(c);
                }

                rows.Add(r);
            }
            dgrid.ItemsSource = rows;

        }

        private void ToggleColumn(Header header, String hoster)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                int entryind = header.Hosters.IndexOf(hoster); //cell entries from this hoster
                var affectedCells = _cells.Where(c => c.Header == header).ToList(); //cells from this header
                bool allChecked = affectedCells.Select(c=>c.Entries.ElementAt(entryind)).Where(e=>e.Enabled).All(e=>e.Checked); //check if all checkboxes for this hoster are checked
                affectedCells.ForEach(c => c.Entries.ForEach(e =>e.Checked=e.Enabled&&((c.Entries.IndexOf(e)==entryind)^ allChecked))); //either turn all others from this header off, or on
                _cells.Where(c => c.Header != header).SelectMany(c => c.Entries).ToList().ForEach(e=>e.Checked= e.Enabled&&allChecked); //either turn all checkboxes of the other headers on or off
            }
            else
            {

                int entryind = header.Hosters.IndexOf(hoster);
                var entries =
                    _cells.Where(c => c.Header == header)
                        .Select(c => c.Entries.ElementAt(entryind))
                        .Where(ce => ce.Enabled)
                        .ToList(); //all cell entries from this hoster&header combo
                bool allChecked = entries.All(e => e.Checked); //check if all checkboxes for this hoster&header are checked
                entries.ForEach(e => e.Checked = !allChecked);
            }
        }

        private void ToggleFullColumn(Header header)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var entries = _cells.Where(c => c.Header == header).SelectMany(c => c.Entries).Where(ce => ce.Enabled).ToList(); //cell entries from this header
                bool allChecked = entries.All(e => e.Checked); //check if all checkboxes for this header are checked
                entries.ForEach(e => e.Checked = !allChecked); //either turn all entries for this header on or off
                _cells.Where(c => c.Header != header).SelectMany(c => c.Entries).ToList().ForEach(e => e.Checked = e.Enabled && allChecked); //either turn all checkboxes of the other headers on or off
            }
            else
            {
                var entries =
                    _cells.Where(c => c.Header == header)
                        .SelectMany(c=>c.Entries)
                        .Where(ce => ce.Enabled)
                        .ToList(); //all cell entries from this header
                bool allChecked = entries.All(e => e.Checked); //check if all checkboxes for this header are checked
                entries.ForEach(e => e.Checked = !allChecked);
            }
        }

        private void ToggleRow(Row row)
        {

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
               var entries = row.Cells.SelectMany(c => c.Entries).Where(ce => ce.Enabled).ToList(); //all cell entries from this row
               bool allChecked = entries.All(e => e.Checked); //check if all checkboxes for this row are checked
               entries.ForEach(e => e.Checked = !allChecked); //either turn all entries for this row off or on
               _cells.Where(c=> !row.Cells.Contains(c)).SelectMany(c=>c.Entries).Where(ce => ce.Enabled).ToList().ForEach(e => e.Checked = allChecked); //either turn all entries for the other rows on or off
            }
            else
            {
                var entries = row.Cells.SelectMany(c => c.Entries).Where(ce => ce.Enabled).ToList(); //all cell entries from this row
                bool allChecked = entries.All(e => e.Checked); //check if all checkboxes for this row are checked
                entries.ForEach(e => e.Checked = !allChecked);
            }
        }

        private void DownloadButtonClick(object sender, RoutedEventArgs rea)
        {   
            _cells.Where(c=>c.Entries.Any(e=>e.Checked)).Select(c=>c.Episode).Distinct().ToList().ForEach(e=>e.Downloaded=true);
            var links = _cells.SelectMany(c => c.Entries).Where(e => e.Checked).Select(e => e.Link).Distinct().ToList();
            var linkstring = String.Join("\r\n",links);


            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(linkstring);
                    Clipboard.Flush();
                    Stats.TrackAction(Stats.TrackActivity.Download);
                    return;
                }
                catch
                {
                    //nah
                }
                Thread.Sleep(10);
            }
            MessageBox.Show("Couldn't Copy link to clipboard.\n" + linkstring);

        }
    }
}
