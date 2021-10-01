#region USING
using Diplomski.Class_s;
using Diplomski.JSON_Class;
using Diplomski.Properties;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
#endregion

namespace Diplomski
{
    public partial class MainWindow : Window
    {
        #region GLOBAL_VALUES
        private bool enableSelected = false; //check if selected anime to preview was selected, to show tab
        private bool areNewsLoaded = false; //check to load news only first time when loading news tab

        private string URL; //string for search results
        private string filterOrText; //check if we are searching by filter or by text
        private string textSearch; //text for searching
        private string filename; //name of the file, usually as a path
        private string filePath; //path of the file
        private string addedtoStatus; //status of the anime while adding, to format for .anikai list
        private string URLForDifferentPages; //base url for searching with a different page
        private string importUsernameString; //username of a person from MyAnimeList.net

        private int rowCounterForSeasons = -1; //row for the selected anime related list
        private int genreSelected = 0; //check if a genre was selected for shaping the URL
        private int currentYear; //get current year
        private int currentPage = 1; //current page for search results
        private int maxPage; //max page for search results
        private int importPageCounter = 1; //300 imported anime per 1 page
        private double[] progressBarCounter = new double[11]; //progress bar score 1-10 (and 0 for not rated)

        private SelectedCodeJSON selectedAnime; //selected anime json code
        private List<CodeJSON> listItems; //results for searching json code
        private List<AnimeImportJSON> importList; //import anime list json code

        private List<NewsItem> newsList; //news rss feed
        private List<Episode> torrentList; //torrent rss feed

        private int lastTimer = 0; //check if last timer to not overlay
        private DispatcherTimer notificationTimer; //notification timer (3sec)
        private DispatcherTimer importTimer; //timer for importing anime (2sec)
        private int lastTabForInternet = 0; //instant swap tab if internet goes off

        private JObject scheduleObject; //schedule json code
        private JObject topObject; //top anime json code

        private string[] lines; //array of rows in the file loaded (through filename)
        private ListSortDirection sortDirection = ListSortDirection.Ascending; //sort direction for lists
        #endregion
        #region MAIN_WINDOW_INITIALIZE_COMPONENT

        #region internet_check_functions
        private void AvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (e.IsAvailable)
                {
                    lbHeader.Content = "";

                    tabHome.IsEnabled = true; tabHome.Foreground = Brushes.White;
                    tabSearch.IsEnabled = true; tabSearch.Foreground = Brushes.White;
                    tabMyList.IsEnabled = true; tabMyList.Foreground = Brushes.White;
                    tabNews.IsEnabled = true; tabNews.Foreground = Brushes.White;
                    tabTorrent.IsEnabled = true; tabTorrent.Foreground = Brushes.White;
                    seasonTab.IsEnabled = true; seasonTab.Foreground = Brushes.White;
                    previewTab.IsEnabled = true;
                    themePicker.Visibility = Visibility.Visible;
                    searchBarExpander.Visibility = Visibility.Visible;

                    btnRetry.Visibility = Visibility.Collapsed;
                    btnRetry.Margin = new Thickness(0, 3, 80, 1);

                    mainTab.SelectedIndex = lastTabForInternet;
                }
                else
                {
                    lbHeader.Content = "NO INTERNET CONNECTION!";

                    tabHome.IsEnabled = false; tabHome.Foreground = Brushes.Gray;
                    tabSearch.IsEnabled = false; tabSearch.Foreground = Brushes.Gray;
                    tabMyList.IsEnabled = false; tabMyList.Foreground = Brushes.Gray;
                    tabNews.IsEnabled = false; tabNews.Foreground = Brushes.Gray;
                    tabTorrent.IsEnabled = false; tabTorrent.Foreground = Brushes.Gray;
                    seasonTab.IsEnabled = false; seasonTab.Foreground = Brushes.Gray;
                    previewTab.IsEnabled = false;
                    themePicker.Visibility = Visibility.Hidden;
                    searchBarExpander.Visibility = Visibility.Hidden;

                    btnRetry.Visibility = Visibility.Visible;
                    btnRetry.Margin = new Thickness(0, 3, 80, 1);

                    lastTabForInternet = mainTab.SelectedIndex;
                    mainTab.SelectedIndex = 7;
                }
            });
        } //change if internet gets connected/disconnected

        [DllImport("wininet.dll")] // dll that checks internet status change
        private extern static bool InternetGetConnectedState(out int description, int reservedValue); 
        public static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            schedule_Panel.Visibility = Visibility.Collapsed;
            previewTab.Visibility = Visibility.Collapsed;
            rbCurrentlyWatching.IsChecked = true;

            NetworkAvailabilityChangedEventHandler myHandler = new NetworkAvailabilityChangedEventHandler(AvailabilityChanged); //internet checker
            NetworkChange.NetworkAvailabilityChanged += myHandler;

            DoubleAnimation ani = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5)); //fade in animation
            this.BeginAnimation(OpacityProperty, ani);

            string color = Settings.Default.color; //set color theme that was last used
            Uri uri = new Uri($"pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor." + color + ".xaml");
            Application.Current.Resources.MergedDictionaries.RemoveAt(2);
            Application.Current.Resources.MergedDictionaries.Insert(2, new ResourceDictionary() { Source = uri });

            currentYear = DateTime.Now.Year; //add years up untill current in combo boxes
            for (int i = currentYear; i >= 1917; i--)
            {
                startYearComboBox.Items.Add(i);
                endYearComboBox.Items.Add(i);
            }
            lbHeader.Content = "";
        }
        #endregion

        #region HEAD_TAB_BUTTONS
        private void btnHome_Click(object sender, RoutedEventArgs e) { mainTab.SelectedIndex = 0; lbHeader.Content = ""; }
        private void btnMyList_Click(object sender, RoutedEventArgs e)
        {
            mainTab.SelectedIndex = 2;

            if (rbCurrentlyWatching.IsChecked == true) { lbHeader.Content = "My List - Currently Watching"; }
            else if (rbCompleted.IsChecked == true) { lbHeader.Content = "My List - Completed"; }
            else if (rbOnHold.IsChecked == true) { lbHeader.Content = "My List - On-Hold"; }
            else if (rbDropped.IsChecked == true) { lbHeader.Content = "My List - Dropped"; }
            else if (rbPlanToWatch.IsChecked == true) { lbHeader.Content = "My List - Plan to Watch"; }
        }
        private void btnNews_Click(object sender, RoutedEventArgs e) { mainTab.SelectedIndex = 3; lbHeader.Content = "News"; if (areNewsLoaded == false) news(); }
        private void btnTorrent_Click(object sender, RoutedEventArgs e) { mainTab.SelectedIndex = 4; lbHeader.Content = "Download Torrent Episodes"; }
        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (enableSelected)
            {
                mainTab.SelectedIndex = 6;
                lbHeader.Content = shortenTitle(selectedAnime.title, 0);
            }
            else { notification("You haven't selected an anime to preview!"); }
        }
        private void btnRetry_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e) { Close(); }
        #endregion
        #region SEARCH_BUTTON_FUNCTION
        private void searchButton_Click(object sender, RoutedEventArgs e) 
        {
            if (startYearComboBox.SelectedIndex >= endYearComboBox.SelectedIndex)
            {
                filterOrText = "filter"; //set type for future searches
                clearResultList(); //clear current list
                SearchForListGenre(); //search
                clearSearchSelected(); //reset filters
                lbHeader.Content = "";
            }
            else
            {
                notification("Starting year is lower than the ending year!");
            }
        } //search button with filter function
        private void searchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchByNameFunction();
            }
        } //search by name textbox (enter key)
        private void SearchByNameFunction()
        {
            if (!string.IsNullOrEmpty(searchTextBox.Text))
            {
                lbHeader.Content = "";
                filterOrText = "text"; //set type for future searches
                clearResultList(); //clear current list
                textSearch = searchTextBox.Text;
                JSONtoList("https://api.jikan.moe/v3/search/anime?&q=" + textSearch + "&page=" + currentPage); //search
                searchTextBox.Text = "";
                mainTab.SelectedIndex = 0;
                changeSearchBarWidth();
            }
        } //search button by name function
        private void changeSearchBarWidth()
        {
            if (searchBarExpander.Width > 50)
            {
                DoubleAnimation aniWidth = new DoubleAnimation(searchBarExpander.Width, 50, TimeSpan.FromSeconds(0.15));
                searchBarExpander.BeginAnimation(MaterialDesignThemes.Wpf.ColorZone.WidthProperty, aniWidth);
            }
            else
            {
                DoubleAnimation aniWidth = new DoubleAnimation(searchBarExpander.Width, 265, TimeSpan.FromSeconds(0.15));
                searchBarExpander.BeginAnimation(MaterialDesignThemes.Wpf.ColorZone.WidthProperty, aniWidth);
                searchTextBox.Focus();
            }
        } // reset search bar width after searching
        private void btnSearchIcon_Click(object sender, RoutedEventArgs e)
        {
            changeSearchBarWidth();
        } //expanding search textbox function

        private void clearResultList()
        {
            foreach (Button btn in searchResultsStackPanel.Children.OfType<Button>())
            {
                btn.Visibility = Visibility.Collapsed;
            }
            searchResultsStackPanel.Children.Clear();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            currentPage = 1;
            sv.ScrollToTop();
        } //reset search result
        private void clearSearchSelected()
        {
            foreach (StackPanel sp in CheckBoxSearchPanel.Children)
            {
                foreach (CheckBox cb in sp.Children)
                {
                    if (cb.IsChecked == true)
                    {
                        cb.IsChecked = false;
                    }
                }
            }

            excludeComboBox.IsChecked = false;
            typeComboBox.SelectedIndex = -1;
            statusComboBox.SelectedIndex = -1;
            ratedComboBox.SelectedIndex = -1;
            startYearComboBox.SelectedIndex = -1;
            endYearComboBox.SelectedIndex = -1;
        } //reset filter function

        private void SearchForListGenre()
        {
            URL = "https://api.jikan.moe/v3/search/anime?order_by=score"; //base url
            foreach (StackPanel sp in CheckBoxSearchPanel.Children)
            {
                foreach (CheckBox cb in sp.Children)
                {
                    if (cb.IsChecked == true)
                    {
                        if (genreSelected == 0) { URL += "&genre=" + cb.Name.Remove(0, 1); genreSelected = 1; } //is at least one selected
                        else { URL += "," + cb.Name.Remove(0, 1); }
                    }
                }
            } //genre
            genreSelected = 0;

            //type - TV - MOVIE - ONE - OVA - SPECIAL - MUSIC
            if (typeComboBox.SelectedIndex == 0) { URL += "&type=tv"; }
            else if (typeComboBox.SelectedIndex == 1) { URL += "&type=movie"; }
            else if (typeComboBox.SelectedIndex == 2) { URL += "&type=ona"; }
            else if (typeComboBox.SelectedIndex == 3) { URL += "&type=ova"; }
            else if (typeComboBox.SelectedIndex == 4) { URL += "&type=special"; }
            else if (typeComboBox.SelectedIndex == 5) { URL += "&type=music"; }

            //status - COMPLETED - AIRING - UPCOMING
            if (statusComboBox.SelectedIndex == 0) { URL += "&status=completed"; }
            else if (statusComboBox.SelectedIndex == 1) { URL += "&status=airing"; }
            else if (statusComboBox.SelectedIndex == 2) { URL += "&status=upcoming"; }

            //rated - G - PG - PG-13 - R17 - R - RX
            if (ratedComboBox.SelectedIndex == 0) { URL += "&rated=g"; }
            else if (ratedComboBox.SelectedIndex == 1) { URL += "&rated=pg"; }
            else if (ratedComboBox.SelectedIndex == 2) { URL += "&rated=pg13"; }
            else if (ratedComboBox.SelectedIndex == 3) { URL += "&rated=r17"; }
            else if (ratedComboBox.SelectedIndex == 4) { URL += "&rated=r"; }
            else if (ratedComboBox.SelectedIndex == 5) { URL += "&rated=rx"; }

            if (startYearComboBox.SelectedIndex == -1 && endYearComboBox.SelectedIndex != -1) { URL += "&start_date=1953-01-01"; } //start year

            if (startYearComboBox.SelectedIndex != -1) { URL += "&start_date=" + startYearComboBox.Text + "-01-01"; } //start year if not selected
            if (endYearComboBox.SelectedIndex != -1) { URL += "&end_date=" + endYearComboBox.Text + "-12-31"; } //end year 

            if (excludeComboBox.IsChecked == true) { URL += "&genre_exclude=0"; } //checkbox if selected genres are excluded from search

            URLForDifferentPages = URL; //url set for searches with different pages
            URL += "&page=" + currentPage; //url set for searches with first page

            if (URL != "https://api.jikan.moe/v3/search/anime/?order_by=score") { JSONtoList(URL); } //search with url formed
        } //form url for searching function

        private void btnResetFilter_Click(object sender, RoutedEventArgs e) { clearSearchSelected(); } // reset filter button
        #endregion
        #region FORM_SEARCH_RESULT_INTO_GRID_FUNCTION
        private void JSONtoList(string url)
        {
            RestClient selectClient = new RestClient();
            selectClient.endPoint = url;

            if (selectClient.makeRequest() != "404")
            {
                welcomeImage.Visibility = Visibility.Hidden;
                mainGridBackground.Background = Brushes.Black;

                mainTab.SelectedIndex = 0;
                JObject parsedObject = JObject.Parse(selectClient.makeRequest());
                listItems = JsonConvert.DeserializeObject<List<CodeJSON>>(parsedObject["results"].ToString());
                CodeJSON maxPageConvert = JsonConvert.DeserializeObject<CodeJSON>(parsedObject.ToString());
                maxPage = maxPageConvert.last_page;

                for (int i = 0; i < listItems.Count; i++)
                {
                    StackPanel spMain = new StackPanel
                    {
                        Width = 1264,
                        Height = 236,
                        Orientation = Orientation.Horizontal,
                        Background = new SolidColorBrush(Color.FromArgb(0xFF, 33, 35, 37)),
                        Margin = new Thickness(0, 2, 5, 3),
                        Cursor = Cursors.Hand,
                        Name = "malid_" + listItems[i].mal_id.ToString(),
                    };
                    spMain.PreviewMouseUp += new MouseButtonEventHandler(spMainClick);

                    Image dynamicImage = new Image
                    {
                        Width = 168,
                        Height = 236,
                        Stretch = Stretch.Fill
                    };
                    imageSource(dynamicImage, listItems[i].image_url.ToString());
                    spMain.Children.Add(dynamicImage);

                    StackPanel spInformation = new StackPanel
                    {
                        Width = 1096,
                        VerticalAlignment = VerticalAlignment.Top,
                        Orientation = Orientation.Vertical
                    };
                    spMain.Children.Add(spInformation);

                    TextBlock tbTitle = new TextBlock
                    {
                        Text = listItems[i].title,
                        Background = Brushes.Transparent,
                        Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Left,
                        FontWeight = FontWeights.Bold,
                        MaxWidth = 800,
                        FontSize = 20,
                        Margin = new Thickness(10,10,20,0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    spInformation.Children.Add(tbTitle);

                    StackPanel spStats = new StackPanel
                    {
                        Width = 1076,
                        Height = 38,
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(10, 0, 10, 0),
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    spInformation.Children.Add(spStats);

                    string type = listItems[i].type; if (!string.IsNullOrEmpty(type)) { type = type + "   "; }
                    string episodes = listItems[i].episodes; if (episodes != "0") { episodes = "Ep: " + episodes; } else { episodes = ""; }
                    string score = listItems[i].score; if (!string.IsNullOrEmpty(score) && score != "0") { score = "   Score: " + score; } else { score = ""; }

                    TextBlock tbInformation = new TextBlock
                    {
                        Text = type + episodes + score,
                        Background = Brushes.Transparent,
                        Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Left,
                        FontWeight = FontWeights.Medium,
                        MaxWidth = 800,
                        Height = 156,
                        FontSize = 20,
                        Margin = new Thickness(0, 10, 10, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        TextWrapping = TextWrapping.Wrap
                    };
                    spStats.Children.Add(tbInformation);
                    addStatusToSeasonItem(listItems[i].title, spStats,150,14);

                    TextBlock tbSynopsis = new TextBlock
                    {
                        Text = listItems[i].synopsis,
                        Background = Brushes.Transparent,
                        Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Left,
                        FontWeight = FontWeights.Medium,
                        MaxWidth = 1030,
                        Height = 156,
                        FontSize = 20,
                        Margin = new Thickness(10, 50, 10, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        TextWrapping = TextWrapping.Wrap
                    };
                    spInformation.Children.Add(tbSynopsis);

                    searchResultsStackPanel.Children.Add(spMain);
                }

                StackPanel spMoreResults = new StackPanel
                {
                    Height = 40,
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(50, 20, 50, 20),
                };
                searchResultsStackPanel.Children.Add(spMoreResults);

                generatePageButton(1, spMoreResults);
                if (currentPage - 1 > 2) generateDots(spMoreResults);
                if (currentPage - 1 > 1) { generatePageButton(currentPage - 1, spMoreResults); } 
                if (currentPage != 1 && currentPage != maxPage) { generatePageButton(currentPage, spMoreResults); } 
                if (currentPage + 1 < maxPage) { generatePageButton(currentPage + 1, spMoreResults); }
                if(maxPage - currentPage > 2) generateDots(spMoreResults);
                if (1 != maxPage && currentPage < maxPage) { generatePageButton(maxPage, spMoreResults); }
            }
            else
            {
                notification("No result!"); 
            }
        } //form search result from url function

        private void generateDots(StackPanel spMoreResults)
        {
            StackPanel sp = new StackPanel
            {
                Width = 40,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 13, 15, 17)),
                Margin = new Thickness(5, 0, 5, 0),
                Cursor = Cursors.Hand
            };
            spMoreResults.Children.Add(sp);

            TextBlock tbCurrentPage = new TextBlock
            {
                Text = "...",
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Medium,
                FontSize = 20,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(tbCurrentPage);
        } // adding ... to bottom of search stack panel
        private void generatePageButton(int cp, StackPanel spMoreResults)
        {
            StackPanel sp = new StackPanel
            {
                Width = 40,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = cp,
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 33, 35, 37)),
                Margin = new Thickness(5, 0, 5, 0),
                Cursor = Cursors.Hand
            };
            if(currentPage == cp) { sp.SetResourceReference(StackPanel.BackgroundProperty, "PrimaryHueMidBrush"); }
            else
            {
                sp.MouseEnter += new MouseEventHandler(moreResultEnter);
                sp.MouseLeave += new MouseEventHandler(moreResultleave);
            }
            sp.PreviewMouseUp += new MouseButtonEventHandler(moreResultClick);
            spMoreResults.Children.Add(sp);

            TextBlock tbCurrentPage = new TextBlock
            {
                Text = cp.ToString(),
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Medium,
                FontSize = 20,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(tbCurrentPage);
        } // adding page number to bottom of search stack panel

        private void moreResultEnter(object sender, MouseEventArgs e) { (sender as StackPanel).SetResourceReference(StackPanel.BackgroundProperty, "PrimaryHueMidBrush"); } //color theme on enter
        private void moreResultleave(object sender, MouseEventArgs e) { (sender as StackPanel).Background = new SolidColorBrush(Color.FromArgb(0xFF, 33, 35, 37)); } //color gray on leave
        private void moreResultClick(object sender, MouseButtonEventArgs e)
        {
            if (currentPage != Convert.ToInt32((sender as StackPanel).Tag))
            {
                (sender as StackPanel).Visibility = Visibility.Collapsed;
                currentPage = Convert.ToInt32((sender as StackPanel).Tag);
                if (filterOrText == "filter")
                {
                    foreach (StackPanel sp in searchResultsStackPanel.Children.OfType<StackPanel>())
                    {
                        sp.Visibility = Visibility.Collapsed;
                    }
                    JSONtoList(URLForDifferentPages + "&page=" + currentPage);
                    sv.ScrollToTop();
                }
                else if (filterOrText == "text")
                {
                    foreach (StackPanel sp in searchResultsStackPanel.Children.OfType<StackPanel>())
                    {
                        sp.Visibility = Visibility.Collapsed;
                    }
                    JSONtoList("https://api.jikan.moe/v3/search/anime?&q=" + textSearch + "&page=" + currentPage);
                    sv.ScrollToTop();
                }
            }
        } //get page number, clear results and create new results for that page number

        private void spMainClick(object sender, RoutedEventArgs e)
        {
            mainTab.SelectedIndex = 6;
            setPreview((sender as StackPanel).Name.Remove(0, 6));
            descriptionTab.SelectedIndex = 0;
            previewTab.Visibility = Visibility.Visible;
        } //preview selected anime
        #endregion
        #region OPTION_BUTTONS_AND_EXTRA_FUNCTIONS
        private void imageSource(Image img, string src)
        {
            img.Source = new BitmapImage(new Uri(src, UriKind.Absolute), new RequestCachePolicy(RequestCacheLevel.BypassCache)) { CacheOption = BitmapCacheOption.OnLoad };
        } //set image function
        private void btnTrailer_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(selectedAnime.trailer_url);
        } //open trailer in app function
        private void btnMal_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(selectedAnime.url);
        } // open MyAnimeList.net for selected anime function
        private void headerPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        } //drag window function
        private void themePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string color = "";

            if (themePicker.SelectedIndex == 0) { color = "Purple"; }
            else if (themePicker.SelectedIndex == 1) { color = "Indigo"; }
            else if (themePicker.SelectedIndex == 2) { color = "Blue"; }
            else if (themePicker.SelectedIndex == 3) { color = "LightBlue"; }
            else if (themePicker.SelectedIndex == 4) { color = "Cyan"; }
            else if (themePicker.SelectedIndex == 5) { color = "Teal"; }
            else if (themePicker.SelectedIndex == 6) { color = "Green"; }
            else if (themePicker.SelectedIndex == 7) { color = "LightGreen"; }
            else if (themePicker.SelectedIndex == 8) { color = "Red"; }
            else if (themePicker.SelectedIndex == 9) { color = "Pink"; }
            else if (themePicker.SelectedIndex == 10) { color = "Brown"; }
            else if (themePicker.SelectedIndex == 11) { color = "Orange"; }
            else if (themePicker.SelectedIndex == 12) { color = "Yellow"; }
            else if (themePicker.SelectedIndex == 13) { color = "Lime"; }

            if (themePicker.SelectedIndex >= 0 && themePicker.SelectedIndex <= 13)
            {
                Uri uri = new Uri($"pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor." + color + ".xaml");
                Application.Current.Resources.MergedDictionaries.RemoveAt(2);
                Application.Current.Resources.MergedDictionaries.Insert(2, new ResourceDictionary() { Source = uri });

                Settings.Default.color = color;
                Settings.Default.Save();
            }
        } //on combobox change, change the theme of the app
        private string shortenTitle(string title, int count)
        {
            if (title.Length >= 28)
            {
                title = title.Substring(0, 28 - count) + "...";
            }
            return title;
        } // return shortened string
        private string setDate(String str)
        {
            if (string.IsNullOrEmpty(str)) { return "TBA"; }
            else
            {
                str = str.Substring(0, 10);
                DateTime asDate = DateTime.ParseExact(str, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                return asDate.ToString("MMM dd, yyyy");
            }
        }  //set date for selected anime

        // NOTIFICATION_FUNCTION
        private void notification(string text)
        {
            lastTimer++;
            notificationText.Text = text;

            ThicknessAnimation ani = new ThicknessAnimation();
            ani.From = new Thickness(0, 0, 0, -65);
            ani.To = new Thickness(0, 0, 0, 0);
            ani.Duration = TimeSpan.FromSeconds(0.15);
            notificationGrid.BeginAnimation(MarginProperty, ani);

            notificationTimer = new DispatcherTimer();
            notificationTimer.Interval = TimeSpan.FromSeconds(3);
            notificationTimer.Tick += notificationTick;
            notificationTimer.Start();
        }
        private void notificationTick(object sender, EventArgs e)
        {
            lastTimer--;
            if (lastTimer == 0)
            {
                ThicknessAnimation ani = new ThicknessAnimation();
                ani.From = new Thickness(0, 0, 0, notificationGrid.Margin.Bottom);
                ani.To = new Thickness(0, 0, 0, -65);
                ani.Duration = TimeSpan.FromSeconds(0.15);
                notificationGrid.BeginAnimation(MarginProperty, ani);
            }
            (sender as DispatcherTimer).Stop();
        }
        #endregion

        #region SELECTED_SET_FUNCTION
        public void setPreview(string id)
        {
            #region INITIALIZATION
            enableSelected = true;
            RestClient selectClient = new RestClient();
            selectClient.endPoint = "https://api.jikan.moe/v3/anime/" + id;
            JObject parsedObject = JObject.Parse(selectClient.makeRequest());
            selectedAnime = JsonConvert.DeserializeObject<SelectedCodeJSON>(parsedObject.ToString());

            lbHeader.Content = shortenTitle(selectedAnime.title, 0);
            #endregion

            #region BASIC_INFORMATION_LIST
            if (selectedAnime.title_english != null) { selectedTitle_English.Text = selectedAnime.title_english; } //title in English
            else { selectedTitle_English.Text = selectedAnime.title; }

            if (selectedAnime.title_japanese != null) { selectedTitle_Japanese.Text = selectedAnime.title_japanese; } //title in Japanese
            else { selectedTitle_Japanese.Text = selectedAnime.title; }

            previewTab.Header = lbHeader.Content.ToString().ToUpper();

            if (!string.IsNullOrEmpty(selectedAnime.type)) { selectedType.Text = selectedAnime.type; } else { selectedType.Text = "TBA"; } //type
            if (!string.IsNullOrEmpty(selectedAnime.source)) { selectedSource.Text = selectedAnime.source; } else { selectedSource.Text = "TBA"; } //source
            if (!string.IsNullOrEmpty(selectedAnime.status)) { selectedType.Text += " (" + selectedAnime.status + ")"; } else { selectedType.Text = " (TBA)"; } //status
            if (!string.IsNullOrEmpty(selectedAnime.episodes)) { selectedEpisodes.Text = selectedAnime.episodes.ToString() + " (" + selectedAnime.duration + ")"; } else { selectedEpisodes.Text = "TBA"; } //episodes
            if (!string.IsNullOrEmpty(selectedAnime.aired.from)) { selectedDate.Text = setDate(selectedAnime.aired.from) + " to " + setDate(selectedAnime.aired.to); } else { selectedDate.Text = "TBA"; } //date
            if (!string.IsNullOrEmpty(selectedAnime.rating)) { selectedRating.Text = selectedAnime.rating; } else { selectedRating.Text = "TBA"; } //rating
            if (!string.IsNullOrEmpty(selectedAnime.score)) { selectedScore.Text = selectedAnime.score.ToString(); } else { selectedScore.Text = "TBA"; } //score

            if (selectedAnime.trailer_url != null) { btnTrailer.Visibility = Visibility.Visible; } //hide trailer button if there is no trailer
            else { btnTrailer.Visibility = Visibility.Collapsed; }

            if (selectedAnime.episodes == "1" && selectedAnime.aired.to == null)
            {
                if (!string.IsNullOrEmpty(selectedAnime.aired.from)) { selectedDate.Text = setDate(selectedAnime.aired.from); }
                else { selectedDate.Text = "TBA"; }
            }
            #endregion
            #region STUDIOS_LIST
            if (selectedAnime.studios != null)
            {
                selectedStudio.Text = "";
                for (int i = 0; i < selectedAnime.studios.Count; i++)
                {
                    selectedStudio.Text += selectedAnime.studios[i].name;
                    if (i != selectedAnime.studios.Count - 1)
                    {
                        selectedStudio.Text += ", ";
                    }
                }
            }
            else { selectedStudio.Text = "TBA"; }
            #endregion
            #region GENRE_LIST
            if (selectedAnime.genres != null)
            {
                selectedGenre.Text = "";
                for (int i = 0; i < selectedAnime.genres.Count; i++)
                {
                    selectedGenre.Text += selectedAnime.genres[i].name;
                    if (i != selectedAnime.genres.Count - 1)
                    {
                        selectedGenre.Text += ", ";
                    }
                }
            }
            else { selectedGenre.Text = "TBA"; }
            #endregion
            #region SYNOPSIS_SET
            selectedSynopsis.Children.Clear();
            TextBlock tbSynopsis = new TextBlock
            {
                Margin = new Thickness(5, 9, 0, 5),
                FontSize = 20,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap
            };
            if (!string.IsNullOrEmpty(selectedAnime.synopsis)) { tbSynopsis.Text = selectedAnime.synopsis; }
            else { tbSynopsis.Text = "TBA"; }
            selectedSynopsis.Children.Add(tbSynopsis);
            #endregion
            #region PICTURE_SET
            var imgUrl = new Uri(selectedAnime.image_url);
            var imageData = new WebClient().DownloadData(imgUrl);

            var bitmapImage = new BitmapImage { CacheOption = BitmapCacheOption.OnLoad };
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(imageData);
            bitmapImage.EndInit();
            selectedImage.Source = bitmapImage;
            #endregion

            #region RELATED_LIST
            seasonList.Children.Clear();

            if (selectedAnime.related.Prequel != null) // PREQUEL
            {
                addRelatedTitle("Prequel: ");
                for (int i = 0; i < selectedAnime.related.Prequel.Count; i++)
                {
                    addRelated(selectedAnime.related.Prequel[i].name, selectedAnime.related.Prequel[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            if (selectedAnime.related.Sequel != null) // SEQUEL
            {
                addRelatedTitle("Sequel: ");
                for (int i = 0; i < selectedAnime.related.Sequel.Count; i++)
                {
                    addRelated(selectedAnime.related.Sequel[i].name, selectedAnime.related.Sequel[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            if (selectedAnime.related.Sidestory != null) // SIDE STORY
            {
                addRelatedTitle("Side Story: ");
                for (int i = 0; i < selectedAnime.related.Sidestory.Count; i++)
                {
                    addRelated(selectedAnime.related.Sidestory[i].name, selectedAnime.related.Sidestory[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            if (selectedAnime.related.Parentstory != null) // PARENT STORY
            {
                addRelatedTitle("Parent Story: ");
                for (int i = 0; i < selectedAnime.related.Parentstory.Count; i++)
                {
                    addRelated(selectedAnime.related.Parentstory[i].name, selectedAnime.related.Parentstory[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            if (selectedAnime.related.Summary != null) // SUMMARY
            {
                addRelatedTitle("Summary: ");
                for (int i = 0; i < selectedAnime.related.Summary.Count; i++)
                {
                    addRelated(selectedAnime.related.Summary[i].name, selectedAnime.related.Summary[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            if (selectedAnime.related.Alternativeversion != null) // ALTERNATIVE VERSION
            {
                addRelatedTitle("Alternative Version: ");
                for (int i = 0; i < selectedAnime.related.Alternativeversion.Count; i++)
                {
                    addRelated(selectedAnime.related.Alternativeversion[i].name, selectedAnime.related.Alternativeversion[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            if (selectedAnime.related.Alternativesetting != null) // ALTERNATIVE SETTING
            {
                addRelatedTitle("Alternative Setting: ");
                for (int i = 0; i < selectedAnime.related.Alternativesetting.Count; i++)
                {
                    addRelated(selectedAnime.related.Alternativesetting[i].name, selectedAnime.related.Alternativesetting[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            if (selectedAnime.related.Other != null) // OTHER
            {
                addRelatedTitle("Other: ");
                for (int i = 0; i < selectedAnime.related.Other.Count; i++)
                {
                    addRelated(selectedAnime.related.Other[i].name, selectedAnime.related.Other[i].mal_id);
                }
                rowCounterForSeasons++;
            }

            rowCounterForSeasons = -1;
            if (selectedAnime.related.Prequel == null && selectedAnime.related.Sequel == null && selectedAnime.related.Sidestory == null &&
                selectedAnime.related.Parentstory == null && selectedAnime.related.Summary == null && selectedAnime.related.Alternativeversion == null &&
                    selectedAnime.related.Alternativesetting == null && selectedAnime.related.Other == null)
            {
                addRelatedTitle("There are no related Anime for this selection!");
                rowCounterForSeasons = -1;
            }

            #endregion
            #region MUSIC LIST
            musicList.Children.Clear();
            int rowCounterForMusic = 2;

            if (selectedAnime.opening_themes.Count != 0 || selectedAnime.ending_themes.Count != 0)
            {
                TextBlock tbOpening = new TextBlock
                {
                    Margin = new Thickness(5, 9, 0, 5),
                    Text = "Note: less popular songs might not give the correct result on YouTube!",
                    FontSize = 20,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    FontWeight = FontWeights.Medium
                };
                musicList.Children.Add(tbOpening);
            }

            if (selectedAnime.opening_themes.Count != 0)
            {
                TextBlock tbOpening = new TextBlock
                {
                    Margin = new Thickness(5, 24 * rowCounterForMusic + 9, 0, 5),
                    Text = "Openings: ",
                    FontSize = 20,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    FontWeight = FontWeights.Medium
                };
                musicList.Children.Add(tbOpening);

                for (int i = 0; i < selectedAnime.opening_themes.Count; i++)
                {
                    rowCounterForMusic++;

                    TextBlock tb = new TextBlock
                    {
                        Margin = new Thickness(25, 24 * rowCounterForMusic + 9, 0, 5),
                        Text = selectedAnime.opening_themes[i],
                        FontSize = 20,
                        Tag = selectedAnime.title + " OP" + (i + 1),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Medium,
                        FontStyle = FontStyles.Italic,
                        Cursor = Cursors.Hand
                    };
                    tb.MouseLeftButtonDown += new MouseButtonEventHandler(musicItemClick);
                    tb.MouseEnter += new MouseEventHandler(seasonOrMusicEnter);
                    tb.MouseLeave += new MouseEventHandler(seasonOrMusicLeave);
                    musicList.Children.Add(tb);
                }
                rowCounterForMusic += 2;
            }

            if (selectedAnime.ending_themes.Count != 0)
            {
                TextBlock tbEnding = new TextBlock
                {
                    Margin = new Thickness(5, 24 * rowCounterForMusic + 9, 0, 5),
                    Text = "Endings: ",
                    FontSize = 20,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    FontWeight = FontWeights.Medium
                };
                musicList.Children.Add(tbEnding);

                for (int i = 0; i < selectedAnime.ending_themes.Count; i++)
                {
                    rowCounterForMusic++;

                    TextBlock tb = new TextBlock
                    {
                        Margin = new Thickness(25, 24 * rowCounterForMusic + 9, 0, 5),
                        Text = selectedAnime.ending_themes[i],
                        FontSize = 20,
                        Tag = selectedAnime.title + " ED" + (i + 1),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Medium,
                        FontStyle = FontStyles.Italic,
                        Cursor = Cursors.Hand
                    };
                    tb.MouseLeftButtonDown += new MouseButtonEventHandler(musicItemClick);
                    tb.MouseEnter += new MouseEventHandler(seasonOrMusicEnter);
                    tb.MouseLeave += new MouseEventHandler(seasonOrMusicLeave);
                    musicList.Children.Add(tb);
                }
            }

            if (selectedAnime.opening_themes.Count == 0 && selectedAnime.ending_themes.Count == 0)
            {
                TextBlock tb = new TextBlock
                {
                    Margin = new Thickness(5, 9, 0, 5),
                    Text = "There are no openings or endings for this anime",
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Medium
                };
                musicList.Children.Add(tb);
            }
            #endregion
        }

        #region FUNCTIONS_FOR_RELATED_AND_MUSIC_LIST
        public void addRelatedTitle(string TitleText)
        {
            rowCounterForSeasons++;

            TextBlock tbTitle = new TextBlock
            {
                Margin = new Thickness(5, 24 * rowCounterForSeasons + 9, 0, 5),
                Text = TitleText,
                FontSize = 20,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                FontWeight = FontWeights.Medium
            };
            seasonList.Children.Add(tbTitle);
        }
        public void addRelated(string name, string id)
        {
            rowCounterForSeasons++;

            TextBlock tb = new TextBlock
            {
                Margin = new Thickness(25, 24 * rowCounterForSeasons + 9, 0, 5),
                Text = name,
                FontSize = 20,
                Name = "tb" + id,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Medium,
                FontStyle = FontStyles.Italic,
                Cursor = Cursors.Hand
            };
            tb.MouseLeftButtonDown += new MouseButtonEventHandler(seasonItemClick);
            tb.MouseEnter += new MouseEventHandler(seasonOrMusicEnter);
            tb.MouseLeave += new MouseEventHandler(seasonOrMusicLeave);
            seasonList.Children.Add(tb);
        }

        private void seasonItemClick(object sender, EventArgs e)
        {
            setPreview((sender as TextBlock).Name.Remove(0, 2));
            relatedScrollViewer.ScrollToTop();
            musicScrollViewer.ScrollToTop();
        }
        private void musicItemClick(object sender, MouseButtonEventArgs e)
        {
            string text = (sender as TextBlock).Text.ToString();

            if (text[0] == '#')
            {
                int firstSpaceIndex = text.IndexOf(" ") + 1;
                text = text.Remove(0, firstSpaceIndex);
            }

            if (text[text.Length - 1] == ')')
            {
                char currentChar = text[text.Length - 1];
                while (currentChar != '(')
                {
                    text = text.Substring(0, text.Length - 1);
                    currentChar = text[text.Length - 1];
                }
                text = text.Substring(0, text.Length - 1);
            }

            text = "https://www.youtube.com/results?search_query=" + Regex.Replace(text, @"[^\u0000-\u007F]+", string.Empty).Replace("()", "").Replace(",", "").Replace(" ", "%20");
            Process.Start("https://www.youtube.com/watch?v=" + getVideoID(text));
        }

        public static String getVideoID(string Url)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(Url);
            myRequest.Method = "GET";
            WebResponse myResponse = myRequest.GetResponse();
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(),
                System.Text.Encoding.UTF8);
            string result = sr.ReadToEnd();
            sr.Close();
            myResponse.Close();

            int pFrom = result.IndexOf("/watch?v=") + "/watch?v=".Length;
            int pTo = pFrom + 11;

            result = result.Substring(pFrom, 11);

            return result;
        }

        private void seasonOrMusicLeave(object sender, MouseEventArgs e)
        {
            (sender as TextBlock).Foreground = Brushes.White;
        }
        private void seasonOrMusicEnter(object sender, MouseEventArgs e)
        {
            (sender as TextBlock).SetResourceReference(TextBlock.ForegroundProperty, "PrimaryHueMidBrush");
        }
        #endregion

        #endregion
        #region MY_LIST_FUNCTIONS

        // BASIC_FUNCTIONS
        private void addToList(string score)
        {
            myListManage();
            bool alreadyAdded = false;

            if (filename != null && File.Exists(filename))
            {
                lbHeader.Content = "My List - Currently Watching";
                lines = File.ReadAllLines(filename);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] words = lines[i].Split(' ');
                    if (words[0] == selectedAnime.mal_id) { alreadyAdded = true; }
                }
                if (!alreadyAdded)
                {
                    string episodes;
                    if (selectedAnime.episodes != null)
                    {
                        episodes = selectedAnime.episodes;
                    }
                    else { episodes = "TBA"; }
                    if(episodes == "0") { episodes = "TBA"; }

                    if(score == "0") { score = "-"; }
                    var data = new Anime { ID = selectedAnime.mal_id, Title = selectedAnime.title, Type = selectedAnime.type, Episodes = episodes, Status = addedtoStatus, Score = score.ToString() };

                    if (addedtoStatus == "CurrentlyWatching") { myListCurrentlyWatching.Items.Add(data); rbCurrentlyWatching.Content = "Currently Watching (" + myListCurrentlyWatching.Items.Count + ")"; }
                    else if (addedtoStatus == "Completed") { myListCompleted.Items.Add(data); rbCompleted.Content = "Completed (" + myListCompleted.Items.Count + ")"; }
                    else if (addedtoStatus == "OnHold") { myListOnHold.Items.Add(data); rbOnHold.Content = "On Hold (" + myListOnHold.Items.Count + ")"; }
                    else if (addedtoStatus == "Dropped") { myListDropped.Items.Add(data); rbDropped.Content = "Dropped (" + myListDropped.Items.Count + ")"; }
                    else if (addedtoStatus == "PlantoWatch") { myListPlanToWatch.Items.Add(data); rbPlanToWatch.Content = "Plan to Watch (" + myListPlanToWatch.Items.Count + ")"; }

                    notification("This anime was added to the list!");

                    using (StreamWriter writer = File.AppendText(filename))
                    {
                        writer.WriteLine(selectedAnime.mal_id + " " + selectedAnime.title.Replace(" ", "%20") + " " + selectedAnime.type + " " + selectedAnime.episodes + " " + addedtoStatus + " 0");
                    }
                }
                else { alreadyAdded = false; notification("This anime is already on this list!"); }
                mainTab.SelectedIndex = 2;
                saveList();
            }
        }
        private void myListManage()
        {
            myListCurrentlyWatching.Items.Clear();
            myListCompleted.Items.Clear();
            myListOnHold.Items.Clear();
            myListDropped.Items.Clear();
            myListPlanToWatch.Items.Clear();

            string fileFinal = Path.GetFileName(filename.ToString().Replace(".anikai", ""));
            notification("List: " + fileFinal);
            whatListSelectedtb.Text = "List: " + fileFinal;

            lines = File.ReadAllLines(filename);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] words = lines[i].Split(' ');
                if (words.Length == 6)
                {
                    if(words[3] == "0") { words[3] = "TBA"; }
                    if(words[5] == "0") { words[5] = "-"; }
                    var data = new Anime { ID = words[0], Title = words[1].Replace("%20", " "), Type = words[2], Episodes = words[3], Status = words[4], Score = words[5] };

                    if (words[4] == "CurrentlyWatching") { myListCurrentlyWatching.Items.Add(data); rbCurrentlyWatching.Content = "Currently Watching (" + myListCurrentlyWatching.Items.Count + ")"; }
                    else if (words[4] == "Completed") { myListCompleted.Items.Add(data); rbCompleted.Content = "Completed (" + myListCompleted.Items.Count + ")"; }
                    else if (words[4] == "OnHold") { myListOnHold.Items.Add(data); rbOnHold.Content = "On Hold (" + myListOnHold.Items.Count + ")"; }
                    else if (words[4] == "Dropped") { myListDropped.Items.Add(data); rbDropped.Content = "Dropped (" + myListDropped.Items.Count + ")"; }
                    else if (words[4] == "PlantoWatch") { myListPlanToWatch.Items.Add(data); rbPlanToWatch.Content = "Plan to Watch (" + myListPlanToWatch.Items.Count + ")"; }
                }
            }
            saveList();
        }

        #region MY_LIST_BUTTON_FUINCTIONS

        // CHANGE_TAB_ON_SELECTED
        private void rbCurrentlyWatching_Checked(object sender, RoutedEventArgs e)
        {
            watchList.SelectedIndex = 0;
            if (lbHeader != null) { lbHeader.Content = "My List - Currently Watching"; }
            setColorOfRadioButtonMyList(rbCurrentlyWatching);
        }
        private void rbCompleted_Checked(object sender, RoutedEventArgs e)
        {
            watchList.SelectedIndex = 1;
            lbHeader.Content = "My List - Completed";
            setColorOfRadioButtonMyList(rbCompleted);
        }
        private void rbOnHold_Checked(object sender, RoutedEventArgs e)
        {
            watchList.SelectedIndex = 2;
            lbHeader.Content = "My List - On-Hold";
            setColorOfRadioButtonMyList(rbOnHold);
        }
        private void rbDropped_Checked(object sender, RoutedEventArgs e)
        {
            watchList.SelectedIndex = 3;
            lbHeader.Content = "My List - Dropped";
            setColorOfRadioButtonMyList(rbDropped);
        }
        private void rbPlanToWatch_Checked(object sender, RoutedEventArgs e)
        {
            watchList.SelectedIndex = 4;
            lbHeader.Content = "My List - Plan to Watch";
            setColorOfRadioButtonMyList(rbPlanToWatch);
        }
        private void setColorOfRadioButtonMyList(RadioButton rb)
        {
            rbCurrentlyWatching.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");
            rbCompleted.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");
            rbOnHold.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");
            rbDropped.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");
            rbPlanToWatch.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");

            rb.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueMidBrush");
        }

        // TOP_DRAWER_FUNCTIONS
        private void updateMyLists()
        {
            myLists.Items.Clear();

            string workingDirectory = Environment.CurrentDirectory;
            string finalDirectory = Directory.GetParent(workingDirectory).Parent.FullName + @"\AnimeLists\";

            DirectoryInfo d = new DirectoryInfo(finalDirectory);
            FileInfo[] Files = d.GetFiles("*.anikai");

            foreach (FileInfo file in Files)
            {
                var lastModified = File.GetLastWriteTime(finalDirectory + file.Name);
                lines = File.ReadAllLines(finalDirectory + file.Name);

                var data = new AnimeListsFromFolder { Listname = file.Name.Replace(".anikai", ""), Count = lines.Length + " Anime", Date = lastModified.ToString("MMM, dd yyyy    hh:mm tt"), path = finalDirectory + file.Name };
                myLists.Items.Add(data);
            }
        }
        private void btnTopDrawerView_Click(object sender, RoutedEventArgs e)
        {
            updateMyLists();
        }

        private void btnTopDrawerOpen_Click(object sender, RoutedEventArgs e)
        {
            openListButtonFunction();
        }
        private void myLists_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null) return;
            openListButtonFunction();
        }
        private void openListButtonFunction()
        {
            if (myLists.SelectedIndex != -1)
            {
                filename = "";

                for (int i = 1; i < progressBarCounter.Length; i++)
                {
                    foreach (StackPanel sp in spForPb.Children.OfType<StackPanel>())
                    {
                        foreach (ProgressBar pb in sp.Children.OfType<ProgressBar>()) { pb.Value = 0; }
                    }
                }
                myListCurrentlyWatching.Items.Clear();
                myListCompleted.Items.Clear();
                myListOnHold.Items.Clear();
                myListDropped.Items.Clear();
                myListPlanToWatch.Items.Clear();

                rbCurrentlyWatching.Content = "Currently Watching (" + myListCurrentlyWatching.Items.Count + ")";
                rbCompleted.Content = "Completed (" + myListCompleted.Items.Count + ")";
                rbOnHold.Content = "On Hold (" + myListOnHold.Items.Count + ")";
                rbDropped.Content = "Dropped (" + myListDropped.Items.Count + ")";
                rbPlanToWatch.Content = "Plan to Watch (" + myListPlanToWatch.Items.Count + ")";

                whatListSelectedtb.Text = "List: " + Path.GetFileName(filename.ToString().Replace(".anikai", ""));

                var row_list = (AnimeListsFromFolder)myLists.SelectedItem;
                if (!string.IsNullOrEmpty(row_list.path)) { filename = row_list.path; }

                myListManage();
                var drawer = DrawerHost.CloseDrawerCommand;
                drawer.Execute(null, null);
            }
            else
            {
                notification("You haven't selected a list!");
            }
        }

        private void btnTopDrawerDelete_Click(object sender, RoutedEventArgs e)
        {
            if (myLists.SelectedIndex != -1)
            {
                var row_list = (AnimeListsFromFolder)myLists.SelectedItem;

                if (filename != row_list.path)
                {
                    File.Delete(row_list.path);
                    updateMyLists();
                    notification("Deleted " + row_list.Listname);
                }
                else
                {
                    notification("Change your list before deleting it!");
                }
            }
            else
            {
                notification("You haven't selected a list!");
            }

        }
        private void btnTopDrawerRename_Click(object sender, RoutedEventArgs e)
        {
            if (myLists.SelectedIndex != -1)
            {
                if (!string.IsNullOrEmpty(tbRename.Text))
                {
                    var row_list = (AnimeListsFromFolder)myLists.SelectedItem;

                    string oldPath = row_list.path;
                    string newPath = row_list.path.Replace(row_list.Listname + ".anikai", tbRename.Text + ".anikai");

                    if (!File.Exists(newPath))
                    {
                        File.Move(oldPath, newPath);
                        if (filename == oldPath)
                        {
                            filename = newPath;
                            whatListSelectedtb.Text = "List: " + Path.GetFileName(filename.ToString().Replace(".anikai", ""));
                        }

                        updateMyLists();
                        tbRename.Text = "";
                    }
                    else
                    {
                        notification("List with that name already exists!");
                    }
                }
            }
        }
        private void myLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (myLists.SelectedIndex != -1)
            {
                var row_list = (AnimeListsFromFolder)myLists.SelectedItem;
                tbRename.Text = row_list.Listname;
            }
        }
        private void btnCreateList_Click(object sender, RoutedEventArgs e)
        {
            string workingDirectory = Environment.CurrentDirectory;
            string filePath = Directory.GetParent(workingDirectory).Parent.FullName + @"\AnimeLists\MyList1.anikai";

            int count = 1;

            while (File.Exists(filePath))
            {
                filePath = filePath.Replace("MyList" + count + ".anikai", "MyList" + (count + 1) + ".anikai");
                count++;
            }

            var myFile = File.Create(filePath);
            myFile.Close();
            updateMyLists();

            notification("Created  MyList" + count);
        }

        // BUTTON_IMPORT_FUNCTION
        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(importUsername.Text))
            {
                importPageCounter = 1;
                importUsernameString = importUsername.Text;
                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                string newPathString = rgx.Replace(importUsernameString, "");


                string workingDirectory = Environment.CurrentDirectory;
                filePath = Directory.GetParent(workingDirectory).Parent.FullName + @"\AnimeLists\" + newPathString + " 1.anikai";
                int count = 1;

                while (File.Exists(filePath))
                {
                    filePath = filePath.Replace(newPathString + " " + count + ".anikai", newPathString + " " + (count + 1) + ".anikai");
                    count++;
                }
                var myFile = File.Create(filePath);
                filename = filePath;
                myFile.Close();

                mainGrid.IsEnabled = false;
                importTimer = new DispatcherTimer();
                importTimer.Interval = TimeSpan.FromSeconds(2);
                importTimer.Tick += importTick;
                importTimer.Start();
            }
        }
        private void importTick(object sender, EventArgs e)
        {
            importCurrentPage(importUsernameString, importPageCounter);
            importPageCounter++;

            if (importList != null)
            {
                if (importList.Count < 300)
                {
                    mainGrid.IsEnabled = true;
                    importTimer.Stop();

                    myListCurrentlyWatching.Items.Clear();
                    myListCompleted.Items.Clear();
                    myListOnHold.Items.Clear();
                    myListDropped.Items.Clear();
                    myListPlanToWatch.Items.Clear();

                    rbCurrentlyWatching.Content = "Currently Watching (" + myListCurrentlyWatching.Items.Count + ")";
                    rbCompleted.Content = "Completed (" + myListCompleted.Items.Count + ")";
                    rbOnHold.Content = "On Hold (" + myListOnHold.Items.Count + ")";
                    rbDropped.Content = "Dropped (" + myListDropped.Items.Count + ")";
                    rbPlanToWatch.Content = "Plan to Watch (" + myListPlanToWatch.Items.Count + ")";

                    for (int i = 1; i < progressBarCounter.Length; i++)
                    {
                        foreach (StackPanel sp in spForPb.Children.OfType<StackPanel>())
                        {
                            foreach (ProgressBar pb in sp.Children.OfType<ProgressBar>()) { pb.Value = 0; }
                        }
                    }

                    string[] lines = File.ReadAllLines(filename);
                    whatListSelectedtb.Text = "List: " + Path.GetFileName(filename.ToString().Replace(".anikai", ""));
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string[] words = lines[i].Split(' ');
                        if (words.Length == 6)
                        {
                            if(words[3] == "0") { words[3] = "TBA"; }
                            if(words[5] == "0") { words[5] = "-"; }
                            var data = new Anime { ID = words[0], Title = words[1].Replace("%20", " "), Type = words[2], Episodes = words[3], Status = words[4], Score = words[5] };

                            if (words[4] == "CurrentlyWatching") { myListCurrentlyWatching.Items.Add(data); rbCurrentlyWatching.Content = "Currently Watching (" + myListCurrentlyWatching.Items.Count + ")"; }
                            else if (words[4] == "Completed") { myListCompleted.Items.Add(data); rbCompleted.Content = "Completed (" + myListCompleted.Items.Count + ")"; }
                            else if (words[4] == "OnHold") { myListOnHold.Items.Add(data); rbOnHold.Content = "On Hold (" + myListOnHold.Items.Count + ")"; }
                            else if (words[4] == "Dropped") { myListDropped.Items.Add(data); rbDropped.Content = "Dropped (" + myListDropped.Items.Count + ")"; }
                            else if (words[4] == "PlantoWatch") { myListPlanToWatch.Items.Add(data); rbPlanToWatch.Content = "Plan to Watch (" + myListPlanToWatch.Items.Count + ")"; }
                        }
                    }
                    updateMyLists();
                    saveList();
                    notification("List imported!");
                }
            }
            else { importTimer.Stop(); mainGrid.IsEnabled = true; }
            importUsername.Text = "";
        }
        private void importCurrentPage(string username, int importPage)
        {
            RestClient importClient = new RestClient();
            importClient.endPoint = "https://api.jikan.moe/v3/user/" + username + "/animelist/all/" + importPage;
            if (importClient.makeRequest() != "404")
            {
                JObject parsedObject = JObject.Parse(importClient.makeRequest());
                importList = JsonConvert.DeserializeObject<List<AnimeImportJSON>>(parsedObject["anime"].ToString());

                notification("Anime currently imported: " + ((importPageCounter - 1) * 300 + importList.Count));
                using (StreamWriter writer = File.AppendText(filename))
                    for (int i = 0; i < importList.Count; i++)
                    {
                        string status = importList[i].watching_status;

                        if (status == "1") { status = "CurrentlyWatching"; }
                        else if (status == "2") { status = "Completed"; }
                        else if (status == "3") { status = "OnHold"; }
                        else if (status == "4") { status = "Dropped"; }
                        else if (status == "6") { status = "PlantoWatch"; }

                        string score = importList[i].score;
                        if(score == "0") { score = "-"; }

                        writer.WriteLine(importList[i].mal_id + " " + importList[i].title.Replace(" ", "%20") + " " +
                            importList[i].type + " " + importList[i].total_episodes + " " + status + " " + score);
                    }
            }
            else
            {
                notification("Can't find the user: '" + importUsername.Text + "', or the list is private.");
                File.Delete(filePath);

                importPageCounter = 0;
            }
        }

        // DELETE_FROM_LIST_FUNCTION
        private void btnDeleteRowFromList_Click(object sender, RoutedEventArgs e)
        {
            deleteRow();
            notification("Removed the selected anime from the list!");
        }
        private void deleteRow()
        {
            if (watchList.SelectedIndex == 0)
            {
                whichTabSelectedDelete(myListCurrentlyWatching);
                rbCurrentlyWatching.Content = "Currently Watching (" + myListCurrentlyWatching.Items.Count + ")";
            }
            else if (watchList.SelectedIndex == 1)
            {
                whichTabSelectedDelete(myListCompleted);
                rbCompleted.Content = "Completed (" + myListCompleted.Items.Count + ")";
            }
            else if (watchList.SelectedIndex == 2)
            {
                whichTabSelectedDelete(myListOnHold);
                rbOnHold.Content = "On Hold (" + myListOnHold.Items.Count + ")";
            }
            else if (watchList.SelectedIndex == 3)
            {
                whichTabSelectedDelete(myListDropped);
                rbDropped.Content = "Dropped (" + myListDropped.Items.Count + ")";
            }
            else if (watchList.SelectedIndex == 4)
            {
                whichTabSelectedDelete(myListPlanToWatch);
                rbPlanToWatch.Content = "Plan to Watch (" + myListPlanToWatch.Items.Count + ")";
            }
        }
        private void whichTabSelectedDelete(DataGrid list)
        {
            var selectedItem = list.SelectedItem;
            if (selectedItem != null)
            {
                list.Items.Remove(selectedItem);
            }
            saveList();
        }

        // VIEW_SELECTED_FROM_LIST_FUNCTION
        private void btnViewSelectedAnimeFromList_Click(object sender, RoutedEventArgs e)
        {
            enableSelected = true;
            if (watchList.SelectedIndex == 0) { whichTabSelectedSelect(myListCurrentlyWatching); }
            else if (watchList.SelectedIndex == 1) { whichTabSelectedSelect(myListCompleted); }
            else if (watchList.SelectedIndex == 2) { whichTabSelectedSelect(myListOnHold); }
            else if (watchList.SelectedIndex == 3) { whichTabSelectedSelect(myListDropped); }
            else if (watchList.SelectedIndex == 4) { whichTabSelectedSelect(myListPlanToWatch); }
        }
        private void whichTabSelectedSelect(DataGrid list)
        {
            var row_list = (Anime)list.SelectedItem;
            setPreview(row_list.ID);
            mainTab.SelectedIndex = 6;
            descriptionTab.SelectedIndex = 0;
            list.SelectedIndex = -1;
            previewTab.Visibility = Visibility.Visible;
        }

        // SAVE_AND_SORT_FUNCTION
        private void saveList()
        {
            for (int i = 0; i < progressBarCounter.Length; i++) { progressBarCounter[i] = 0; }

            File.WriteAllText(filename, String.Empty);
            sortAndSave(myListCurrentlyWatching);
            sortAndSave(myListCompleted);
            sortAndSave(myListOnHold);
            sortAndSave(myListDropped);
            sortAndSave(myListPlanToWatch);

            progressBarCounter[0] = 0;
            for (int i = 1; i < progressBarCounter.Length; i++)
            {
                foreach (StackPanel sp in spForPb.Children.OfType<StackPanel>())
                {
                    foreach (TextBlock tb in sp.Children.OfType<TextBlock>())
                    {
                        if (tb.Name == "tbpb" + i) { tb.Text = progressBarCounter[i].ToString(); }
                    }
                    foreach (ProgressBar pb in sp.Children.OfType<ProgressBar>())
                    {
                        if ((progressBarCounter[i]) != 0) { if (pb.Name == "pb" + i) { pb.Value = 100.0 / (progressBarCounter.Max() / progressBarCounter[i]); } }
                    }
                }
            }
            double average = 0, pbcounter = 0;
            for (int i = 0; i < progressBarCounter.Length; i++)
            {
                average += progressBarCounter[i] * i;
                pbcounter += progressBarCounter[i];
            }
            tbpbInformation.Text = "Rated: " + pbcounter + "     Average: " + Math.Round(average / pbcounter, 2);
        }
        private void sortAndSave(DataGrid list)
        {
            list.Items.SortDescriptions.Clear();

            var column = list.Columns[1];
            list.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Ascending));
            foreach (var col in list.Columns) { col.SortDirection = null; }
            column.SortDirection = ListSortDirection.Ascending;
            list.Items.Refresh();

            for (int i = 0; i < list.Items.Count; i++)
            {
                var row_list = (Anime)list.Items[i];
                using (StreamWriter writer = File.AppendText(filename))
                {
                    writer.WriteLine(row_list.ID + " " + row_list.Title.Replace(" ", "%20") + " " + row_list.Type + " "
                        + row_list.Episodes + " " + row_list.Status + " " + row_list.Score);
                    if (list != myListPlanToWatch && row_list.Score != "-") { progressBarCounter[Convert.ToInt32(row_list.Score)] += 1; }
                }
            }
        }

        // CHANGE_STATUS_FUNCTION
        private void changeStatus(DataGrid list)
        {
            if (watchList.SelectedIndex == 0) { changeStatusList(myListCurrentlyWatching, list); }
            else if (watchList.SelectedIndex == 1) { changeStatusList(myListCompleted, list); }
            else if (watchList.SelectedIndex == 2) { changeStatusList(myListOnHold, list); }
            else if (watchList.SelectedIndex == 3) { changeStatusList(myListDropped, list); }
            else if (watchList.SelectedIndex == 4) { changeStatusList(myListPlanToWatch, list); }
        }
        private void changeStatusList(DataGrid list, DataGrid ListtoAdd)
        {
            string score = "-";
            if (cbChangeScore.SelectedIndex > 0) { score = cbChangeScore.SelectedIndex.ToString(); }

            var row_list = (Anime)list.SelectedItem;
            var data = new Anime { ID = row_list.ID, Title = row_list.Title, Type = row_list.Type, Episodes = row_list.Episodes, Status = addedtoStatus, Score = score };

            ListtoAdd.Items.Add(data);

            string listName = ListtoAdd.Name;
            if (listName == "myListCurrentlyWatching") { listName = "Currently Watching"; }
            else if (listName == "myListCompleted") { listName = "Completed"; }
            else if (listName == "myListOnHold") { listName = "On Hold"; }
            else if (listName == "myListDropped") { listName = "Dropped"; }
            else if (listName == "myListPlanToWatch") { listName = "Plan to Watch"; }

            string title = row_list.Title;
            if (title.Length > 35) { title = title.Substring(0, 34) + "..."; }
            notification("Successfully edited!");
        }

        // PUT_INFORMATION_INTO_RIGHT_DRAWER_FUNCTION
        private void myList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null) return;
            if (myListCurrentlyWatching.SelectedIndex != -1 || myListCompleted.SelectedIndex != -1 || myListOnHold.SelectedIndex != -1 ||
                myListDropped.SelectedIndex != -1 || myListPlanToWatch.SelectedIndex != -1)
            {
                if (watchList.SelectedIndex == 0)
                {
                    fillInformationFromList(myListCurrentlyWatching);
                    cbChangeStatus.SelectedIndex = 0;
                }
                else if (watchList.SelectedIndex == 1)
                {
                    fillInformationFromList(myListCompleted);
                    cbChangeStatus.SelectedIndex = 1;
                }
                else if (watchList.SelectedIndex == 2)
                {
                    fillInformationFromList(myListOnHold);
                    cbChangeStatus.SelectedIndex = 2;
                }
                else if (watchList.SelectedIndex == 3)
                {
                    fillInformationFromList(myListDropped);
                    cbChangeStatus.SelectedIndex = 3;
                }
                else if (watchList.SelectedIndex == 4)
                {
                    fillInformationFromList(myListPlanToWatch);
                    cbChangeStatus.SelectedIndex = 4;
                }
                btnEdit.Command?.Execute(btnEdit.CommandParameter);
            }
        }
        private void fillInformationFromList(DataGrid list)
        {
            var row_list = (Anime)list.SelectedItem;
            if (row_list.Score != "-") 
            {
                if(Convert.ToInt32(row_list.Score) > 0)
                {
                    cbChangeScore.SelectedIndex = Convert.ToInt32(row_list.Score);
                }
            }
            else { cbChangeScore.SelectedIndex = 0; }
        }

        // ADDING_TO_THE_LIST_FUNCTION
        private void btnAddToList_Click(object sender, RoutedEventArgs e) 
        { 
            myListManage();
            if (!string.IsNullOrEmpty(filename))
            {
                addedtoStatus = "PlantoWatch";
                addToList("-");
                watchList.SelectedIndex = 4;
                rbPlanToWatch.IsChecked = true;
            }
        }

        // SAVE_CHANGES_FUNCTION
        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (cbChangeStatus.SelectedIndex == 0)
            {
                addedtoStatus = "CurrentlyWatching";

                changeStatus(myListCurrentlyWatching);
                deleteRow();

                watchList.SelectedIndex = 0;
                rbCurrentlyWatching.IsChecked = true;
                saveList();
                rbCurrentlyWatching.Content = "Currently Watching (" + myListCurrentlyWatching.Items.Count + ")";
            }
            else if (cbChangeStatus.SelectedIndex == 1)
            {
                addedtoStatus = "Completed";

                changeStatus(myListCompleted);
                deleteRow();

                watchList.SelectedIndex = 1;
                rbCompleted.IsChecked = true;
                saveList();
                rbCompleted.Content = "Completed (" + myListCompleted.Items.Count + ")";
            }
            else if (cbChangeStatus.SelectedIndex == 2)
            {
                addedtoStatus = "OnHold";

                changeStatus(myListOnHold);
                deleteRow();

                watchList.SelectedIndex = 2;
                rbOnHold.IsChecked = true;
                saveList();
                rbOnHold.Content = "On Hold (" + myListOnHold.Items.Count + ")";
            }
            else if (cbChangeStatus.SelectedIndex == 3)
            {
                addedtoStatus = "Dropped";

                changeStatus(myListDropped);
                deleteRow();

                watchList.SelectedIndex = 3;
                rbDropped.IsChecked = true;
                saveList();
                rbDropped.Content = "Dropped (" + myListDropped.Items.Count + ")";
            }
            else if (cbChangeStatus.SelectedIndex == 4)
            {
                addedtoStatus = "PlantoWatch";

                changeStatus(myListPlanToWatch);
                deleteRow();

                watchList.SelectedIndex = 4;
                rbPlanToWatch.IsChecked = true;
                saveList();
                rbPlanToWatch.Content = "Plan to Watch (" + myListPlanToWatch.Items.Count + ")";
            }
        }

        // SORT_BUTTON
        private void btnSortByScore_Click(object sender, RoutedEventArgs e)
        {
            if (sortDirection == ListSortDirection.Descending) { sortDirection = ListSortDirection.Ascending; }
            else { sortDirection = ListSortDirection.Descending; }

            SortDataGrid(myListCurrentlyWatching, "score", sortDirection);
            SortDataGrid(myListCompleted, "score", sortDirection);
            SortDataGrid(myListOnHold, "score", sortDirection);
            SortDataGrid(myListDropped, "score", sortDirection);
            SortDataGrid(myListPlanToWatch, "score", sortDirection);
        }
        public static void SortDataGrid(DataGrid dataGrid, string direction, ListSortDirection sortDirection)
        {
            

            SortAnimeClass[] rows = new SortAnimeClass[dataGrid.Items.Count];
            for (int i = 0; i < dataGrid.Items.Count; i++)
            {
                var row_list = (Anime)dataGrid.Items[i];
                rows[i] = new SortAnimeClass(row_list.ID, row_list.Title, row_list.Score, row_list.Type, row_list.Episodes, row_list.Status);
            }

            dataGrid.Items.Clear();

            if (direction == "score")
            {
                if (sortDirection == ListSortDirection.Ascending)
                {
                    foreach (var sortRow in rows.OrderByDescending(row => row.Score, new SemiNumericComparer()))
                    {
                        var data = new Anime { ID = sortRow.ID, Title = sortRow.Title, Type = sortRow.Type, Episodes = sortRow.Episodes, Status = sortRow.Status, Score = sortRow.Score };
                        dataGrid.Items.Add(data);
                    }
                }
                if (sortDirection == ListSortDirection.Descending)
                {
                    foreach (var sortRow in rows.OrderBy(row => row.Score, new SemiNumericComparer()))
                    {
                        var data = new Anime { ID = sortRow.ID, Title = sortRow.Title, Type = sortRow.Type, Episodes = sortRow.Episodes, Status = sortRow.Status, Score = sortRow.Score };
                        dataGrid.Items.Add(data);
                    }
                }
            }
            else if (direction == "episodes")
            {
                if (sortDirection == ListSortDirection.Ascending)
                {
                    foreach (var sortRow in rows.OrderByDescending(row => row.Episodes, new SemiNumericComparer()))
                    {
                        var data = new Anime { ID = sortRow.ID, Title = sortRow.Title, Type = sortRow.Type, Episodes = sortRow.Episodes, Status = sortRow.Status, Score = sortRow.Score };
                        dataGrid.Items.Add(data);
                    }
                }
                if (sortDirection == ListSortDirection.Descending)
                {
                    foreach (var sortRow in rows.OrderBy(row => row.Episodes, new SemiNumericComparer()))
                    {
                        var data = new Anime { ID = sortRow.ID, Title = sortRow.Title, Type = sortRow.Type, Episodes = sortRow.Episodes, Status = sortRow.Status, Score = sortRow.Score };
                        dataGrid.Items.Add(data);
                    }
                }
            }
        }
        public class SemiNumericComparer : IComparer<string>
        {
            public static bool IsNumeric(string value)
            {
                return int.TryParse(value, out _);
            }

            public int Compare(string s1, string s2)
            {
                const int S1GreaterThanS2 = 1;
                const int S2GreaterThanS1 = -1;

                var IsNumeric1 = IsNumeric(s1);
                var IsNumeric2 = IsNumeric(s2);

                if (IsNumeric1 && IsNumeric2)
                {
                    var i1 = Convert.ToInt32(s1);
                    var i2 = Convert.ToInt32(s2);

                    if (i1 < i2)
                    {
                        return S1GreaterThanS2;
                    }

                    if (i1 > i2)
                    {
                        return S2GreaterThanS1;
                    }
                    return 0;
                }

                if (IsNumeric1) { return S2GreaterThanS1; }
                if (IsNumeric2) { return S1GreaterThanS2; }

                return string.Compare(s1, s2, true, CultureInfo.InvariantCulture);
            }
        }

        private void btnSortByTitle_Click(object sender, RoutedEventArgs e) { sortAllDataGrids(1); } // title
        private void btnSortByType_Click(object sender, RoutedEventArgs e) { sortAllDataGrids(3); } // type
        private void btnSortByEpisodes_Click(object sender, RoutedEventArgs e)
        {
            if (sortDirection == ListSortDirection.Descending) { sortDirection = ListSortDirection.Ascending; }
            else { sortDirection = ListSortDirection.Descending; }

            SortDataGrid(myListCurrentlyWatching, "episodes", sortDirection);
            SortDataGrid(myListCompleted, "episodes", sortDirection);
            SortDataGrid(myListOnHold, "episodes", sortDirection);
            SortDataGrid(myListDropped, "episodes", sortDirection);
            SortDataGrid(myListPlanToWatch, "episodes", sortDirection);
        } // episodes

        private static void SortDataGrid(DataGrid dataGrid, int columnIndex = 0, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            var column = dataGrid.Columns[columnIndex];

            dataGrid.Items.SortDescriptions.Clear();
            dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            foreach (var col in dataGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = sortDirection;

            dataGrid.Items.Refresh();
        }

        private void sortAllDataGrids(int index)
        {
            if(sortDirection == ListSortDirection.Descending) { sortDirection = ListSortDirection.Ascending; }
            else { sortDirection = ListSortDirection.Descending; }

            SortDataGrid(myListCurrentlyWatching, index, sortDirection);
            SortDataGrid(myListCompleted, index, sortDirection);
            SortDataGrid(myListOnHold, index, sortDirection);
            SortDataGrid(myListDropped, index, sortDirection);
            SortDataGrid(myListPlanToWatch, index, sortDirection);
        }
        #endregion
        #endregion
        #region NEWS_SET_FUNCTION
        private void news()
        {
            areNewsLoaded = true;
            NewsList.Children.Clear();

            const string newsURL = "https://www.animenewsnetwork.com/all/rss.xml?ann-edition=w";

            XDocument doc = XDocument.Load(newsURL);
            XElement channel = doc.Root;
            XNamespace ns = channel.GetDefaultNamespace();

            newsList = doc.Descendants(ns + "item").Select(x => new NewsItem()
            {
                link = (string)x.Element(ns + "link"),
                title = (string)x.Element(ns + "title"),
                pubDate = (string)x.Element(ns + "pubDate"),
                description = (string)x.Element(ns + "description"),
                category = (string)x.Element(ns + "category")
            }).ToList();

            #region BASE_LAYOUT_SET
            for (int i = 0; i < newsList.Count; i++)
            {
                StackPanel sp = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 33, 35, 37)),
                    Margin = new Thickness(10, 5, 10, 5),
                    Tag = newsList[i].link,
                    Cursor = Cursors.Hand
                };
                sp.MouseLeftButtonDown += new MouseButtonEventHandler(newsItemClick);
                NewsList.Children.Add(sp);

                Grid gFiller = new Grid
                {
                    Height = 5
                };
                gFiller.SetResourceReference(Grid.BackgroundProperty, "PrimaryHueMidBrush");
                sp.Children.Add(gFiller);

                TextBlock tb = new TextBlock
                {
                    Text = newsList[i].title,
                    FontWeight = FontWeights.Bold,
                    FontSize = 20,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10, 10, 10, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MaxWidth = 1200
                };
                sp.Children.Add(tb);

                string category = newsList[i].category;
                if (!string.IsNullOrEmpty(category)) category = " for " + category;

                TextBlock tbDescription = new TextBlock
                {
                    Text = "Posted on " + DateTime.Parse(newsList[i].pubDate) + category + "\n" + newsList[i].description.Replace("<cite>", "").Replace("</cite>", ""),
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 1200,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(10),
                    Foreground = Brushes.Gray
                };
                sp.Children.Add(tbDescription);
            }
            #endregion
        }
        private void newsItemClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start((sender as StackPanel).Tag.ToString());
        }
        #endregion
        #region TORRENT_SET_FUNCTION

        public void torrentDownload()
        {
            torrentDataGrid.Items.Clear();

            string resolution = cbTorrentResolution.Text;
            if (resolution == "Any Resolution") { resolution = ""; }
            else { resolution = "+" + resolution; }

            string site = cbTorrentSite.Text;
            if (site == "Any Site") { site = ""; }
            else { site = "+" + site; }

            string newsURL = "https://nyaa.si/?page=rss&q=" + site + "+" + tbTorrentSearch.Text + resolution + "&c=1_0&f=0";

            Console.WriteLine(newsURL);
            XDocument doc = XDocument.Load(newsURL);
            XElement channel = doc.Root;
            XNamespace ns = channel.GetDefaultNamespace();
            XNamespace nsSubsPlease = channel.GetNamespaceOfPrefix("nyaa");

            torrentList = doc.Descendants(ns + "item").Select(x => new Episode()
            {
                link = (string)x.Element(nsSubsPlease + "infoHash"),
                title = (string)x.Element(ns + "title"),
                pubDate = (string)x.Element(ns + "pubDate"),
                size = (string)x.Element(nsSubsPlease + "size"),
                seed = (string)x.Element(nsSubsPlease + "seeders") + " / " + (string)x.Element(nsSubsPlease + "leechers")
            }).ToList();

            for (int i = 0; i < torrentList.Count; i++)
            {
                var data = new Episode { title = torrentList[i].title.Replace("[" + cbTorrentSite.Text + "] ", ""), pubDate = DateTime.Parse(torrentList[i].pubDate).ToString(), size = torrentList[i].size, seed = torrentList[i].seed, link = torrentList[i].link };
                torrentDataGrid.Items.Add(data);
            }
            if (torrentList.Count == 0) { notification("There are no results"); }
        } //basic main function

        private void btnTorrentSelected_Click(object sender, RoutedEventArgs e)
        {
            if (torrentDataGrid.SelectedIndex != -1) { Process.Start("magnet:?xt=urn:sha1:" + torrentList[torrentDataGrid.SelectedIndex].link); }
            else { notification("No episode selected for torrenting!"); }
        } //download selected function
        private void btnTorrentSearch_Click(object sender, RoutedEventArgs e)
        {
            torrentDownload();
        } //search torrent by button click function
        private void tbTorrentSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                torrentDownload();
            }
        } //search torrent by enter key function
        #endregion
        #region SEASON_SET_FUNCTION

        #region Schedule_Section
        private void titleThisSeason_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(filename))
            {
                setColorOfRadioButtonSeasons(titleThisSeason);
                foreach (WrapPanel wp in schedule_Panel.Children.OfType<WrapPanel>())
                {
                    wp.Children.Clear();
                }
                scheduleScrollViewer.ScrollToTop();
                seasonTabControl.SelectedIndex = 0;

                LoadCurrentSeasonSchedule();
                schedule_Panel.Visibility = Visibility.Visible;
            }
            else { 
                notification("You need to load a list first.");
                titleThisSeason.IsChecked = false;
            }
        } // check schedule
        private void LoadCurrentSeasonSchedule()
        {
            RestClient scheduleClient = new RestClient();
            scheduleClient.endPoint = "https://api.jikan.moe/v3/schedule";
            scheduleObject = JObject.Parse(scheduleClient.makeRequest());

            FillInformationForSchedule("monday", warpPanel_Monday, spMonday);
            FillInformationForSchedule("tuesday", warpPanel_Tuesday, spTuesday);
            FillInformationForSchedule("wednesday", warpPanel_Wednesday, spWednesday);
            FillInformationForSchedule("thursday", warpPanel_Thursday, spThursday);
            FillInformationForSchedule("friday", warpPanel_Friday, spFriday);
            FillInformationForSchedule("saturday", warpPanel_Saturday, spSaturday);
            FillInformationForSchedule("sunday", warpPanel_Sunday, spSunday);
            FillInformationForSchedule("other", warpPanel_Other, spOther);
            FillInformationForSchedule("unknown", warpPanel_Unknown, spUnknown);
        } //load schedule
        private void FillInformationForSchedule(string day, WrapPanel panel, StackPanel header)
        {
            List<Schedule> schedule = JsonConvert.DeserializeObject<List<Schedule>>(scheduleObject[day].ToString());
            panel.Children.Clear();

            bool anyAnimeThisDay = false;
            for (int i = 0; i < schedule.Count; i++)
            {
                if (!string.IsNullOrEmpty(filename))
                {
                    lines = File.ReadAllLines(filename);
                    for (int x = 0; x < lines.Length; x++)
                    {
                        string[] words = lines[x].Split(' ');
                        if (words[1].Replace("%20", " ") == schedule[i].title.ToString() && words[4] == "CurrentlyWatching")
                        {
                            anyAnimeThisDay = true;
                            StackPanel sp = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                Margin = new Thickness(10, 5, 10, 5),
                                Background = new SolidColorBrush(Color.FromArgb(0xFF, 28, 30, 32))
                            };
                            panel.Children.Add(sp);

                            Image dynamicImage = new Image
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Height = 319,
                                Width = 225,
                                Name = "image_" + schedule[i].mal_id.ToString(),
                                Stretch = Stretch.Fill,
                                Cursor = Cursors.Hand
                            };
                            imageSource(dynamicImage, schedule[i].image_url.ToString());

                            dynamicImage.PreviewMouseUp += new MouseButtonEventHandler(imageClick);
                            dynamicImage.MouseEnter += new MouseEventHandler(imageScheduleEnter);
                            dynamicImage.MouseLeave += new MouseEventHandler(imageScheduleLeave);
                            sp.Children.Add(dynamicImage);

                            string score = schedule[i].score.ToString();
                            if (!string.IsNullOrEmpty(score)) { score = "Score: " + score; }

                            string type = schedule[i].type.ToString();
                            if (!string.IsNullOrEmpty(type)) { type = "   " + type; }

                            string episodes = schedule[i].episodes.ToString();
                            if (!string.IsNullOrEmpty(episodes)) { episodes = "   " + episodes + "ep"; if (type == "   Movie") { episodes = ""; } }

                            string date = schedule[i].airing_start;
                            DateTime dt = new DateTime();
                            if (!string.IsNullOrEmpty(date)) { dt = Convert.ToDateTime(date); }
                            else { date = "Unknown Start Date"; }
                            date = dt.ToString();

                            TextBlock tb = new TextBlock
                            {
                                Text = schedule[i].title.ToString() + "\n" + date + "\n" + score + type + episodes,
                                FontWeight = FontWeights.Medium,
                                FontSize = 16,
                                MaxWidth = 215,
                                Foreground = Brushes.White,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(5),
                                TextAlignment = TextAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                            };
                            sp.Children.Add(tb);
                        }
                    }
                }
            }
            if (anyAnimeThisDay == false)
            {
                header.Visibility = Visibility.Collapsed;
            }
        } //fill information for schedule
        #endregion
        #region Search_Season_Section
        private void titleBrowseSeasons_Checked(object sender, RoutedEventArgs e)
        {
            warpPanel_SelectedSeason.Children.Clear();
            seasonTabControl.SelectedIndex = 1;

            setColorOfRadioButtonSeasons(titleBrowseSeasons);
        } // check browse seasons
        private void seasonYear_tb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        } //prevent letters from year text box
        private void btnSearchSelectedSeason_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(seasonYear_tb.Text))
            {
                if (Convert.ToInt32(seasonYear_tb.Text) >= 1917 && Convert.ToInt32(seasonYear_tb.Text) <= (currentYear + 1))
                {
                    if (!string.IsNullOrEmpty(seasonYear_tb.Text) && seasonPeriod_ComboBox.SelectedIndex != -1)
                    {
                        RestClient selectedSeason = new RestClient();
                        selectedSeason.endPoint = "https://api.jikan.moe/v3/season/" + seasonYear_tb.Text + "/" + seasonPeriod_ComboBox.Text.ToLower();
                        scheduleObject = JObject.Parse(selectedSeason.makeRequest());
                        List<Schedule> selectedSeasonList = JsonConvert.DeserializeObject<List<Schedule>>(scheduleObject["anime"].ToString());
                        whatSeason_TextBox.Text = seasonPeriod_ComboBox.Text + " " + seasonYear_tb.Text + " Anime";

                        warpPanel_SelectedSeason.Children.Clear();
                        for (int i = 0; i < selectedSeasonList.Count; i++)
                        {
                            StackPanel sp = new StackPanel
                            {
                                Orientation = Orientation.Vertical,
                                Margin = new Thickness(10, 5, 10, 5),
                                Background = new SolidColorBrush(Color.FromArgb(0xFF, 28, 30, 32))
                            };
                            warpPanel_SelectedSeason.Children.Add(sp);

                            Image dynamicImage = new Image
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Height = 319,
                                Width = 225,
                                Name = "image_" + selectedSeasonList[i].mal_id.ToString(),
                                Stretch = Stretch.Fill,
                                Cursor = Cursors.Hand
                            };
                            imageSource(dynamicImage, selectedSeasonList[i].image_url.ToString());

                            dynamicImage.PreviewMouseUp += new MouseButtonEventHandler(imageClick);
                            dynamicImage.MouseEnter += new MouseEventHandler(imageScheduleEnter);
                            dynamicImage.MouseLeave += new MouseEventHandler(imageScheduleLeave);
                            sp.Children.Add(dynamicImage);

                            string score = selectedSeasonList[i].score.ToString();
                            if (!string.IsNullOrEmpty(score)) { score = "Score: " + score; }

                            string type = selectedSeasonList[i].type.ToString();
                            if (!string.IsNullOrEmpty(type)) { type = "   " + type; }

                            string episodes = selectedSeasonList[i].episodes.ToString();
                            if (!string.IsNullOrEmpty(episodes)) { episodes = "   " + episodes + "ep"; if (type == "   Movie") { episodes = ""; } }

                            string date = selectedSeasonList[i].airing_start;
                            if (!string.IsNullOrEmpty(date))
                            {
                                DateTime dt = new DateTime();
                                if (!string.IsNullOrEmpty(date)) { dt = Convert.ToDateTime(date); }
                                else { date = "Unknown Start Date\n"; }
                                date = dt.ToString() + "\n";
                            }

                            TextBlock tb = new TextBlock
                            {
                                Text = selectedSeasonList[i].title.ToString() + "\n" + date + score + type + episodes,
                                FontWeight = FontWeights.Medium,
                                FontSize = 16,
                                MaxWidth = 215,
                                Foreground = Brushes.White,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(5),
                                TextAlignment = TextAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                            };
                            addStatusToSeasonItem(selectedSeasonList[i].title.ToString(), sp,225,16);
                            sp.Children.Add(tb);
                        }
                    }
                    else
                    {
                        notification("Select a valid year and season!");
                    }
                }
                else
                {
                    notification("Valid options for year are from 1917 to " + (currentYear + 1) + ".");
                }
            }
        } //browse seasons search button click
        #endregion
        #region Top_Anime_Section
        private void titleTopThisSeason_Checked(object sender, RoutedEventArgs e)
        {
            warpPanel_TopThisSeason.Children.Clear();
            seasonTabControl.SelectedIndex = 2;
            setColorOfRadioButtonSeasons(titleTopThisSeason);
        } //check top
        private void topAnimeSelectionFillInformation(WrapPanel wp, string str)
        {
            wp.Children.Clear();
            RestClient topSelection = new RestClient();
            topSelection.endPoint = "https://api.jikan.moe/v3/top/anime/1/" + str;
            topObject = JObject.Parse(topSelection.makeRequest());
            List<TopAnime> topAnimeList = JsonConvert.DeserializeObject<List<TopAnime>>(topObject["top"].ToString());

            for (int i = 0; i < topAnimeList.Count; i++)
            {
                StackPanel sp = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10, 5, 10, 5),
                    Background = new SolidColorBrush(Color.FromArgb(0xFF, 28, 30, 32))
                };
                wp.Children.Add(sp);

                TextBlock tb1 = new TextBlock
                {
                    Text = "#" + topAnimeList[i].rank,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                sp.Children.Add(tb1);

                Image dynamicImage = new Image
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Height = 319,
                    Width = 225,
                    Name = "image_" + topAnimeList[i].mal_id.ToString(),
                    Stretch = Stretch.Fill,
                    Cursor = Cursors.Hand
                };
                imageSource(dynamicImage, topAnimeList[i].image_url.ToString());

                dynamicImage.PreviewMouseUp += new MouseButtonEventHandler(imageClick);
                dynamicImage.MouseEnter += new MouseEventHandler(imageScheduleEnter);
                dynamicImage.MouseLeave += new MouseEventHandler(imageScheduleLeave);
                sp.Children.Add(dynamicImage);

                string score = topAnimeList[i].score.ToString();
                if (score != "0") { score = "Score: " + score + "   "; }
                else { score = ""; }

                string episodes = topAnimeList[i].episodes.ToString();
                if (!string.IsNullOrEmpty(episodes)) { episodes = "   " + episodes + "ep"; if (topAnimeList[i].type.ToString() == "   Movie") { episodes = ""; } }

                string date = topAnimeList[i].start_date;
                if (string.IsNullOrEmpty(date)) { date = "No Start Date Set"; }

                TextBlock tb2 = new TextBlock
                {
                    Text = topAnimeList[i].title.ToString() + "\n" + date + "\n" + score + topAnimeList[i].type.ToString() + episodes,
                    FontWeight = FontWeights.Medium,
                    MaxWidth = 215,
                    FontSize = 16,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                addStatusToSeasonItem(topAnimeList[i].title.ToString(), sp,225,16);
                sp.Children.Add(tb2);
            }
        } //fill information to top
        private void topAnimeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (topAnimeSelection.SelectedIndex == 0) { topAnimeSelectionFillInformation(warpPanel_TopThisSeason, "airing"); }
            else if (topAnimeSelection.SelectedIndex == 1) { topAnimeSelectionFillInformation(warpPanel_TopThisSeason, "upcoming"); }
            else if (topAnimeSelection.SelectedIndex == 2) { topAnimeSelectionFillInformation(warpPanel_TopThisSeason, "tv"); }
            else if (topAnimeSelection.SelectedIndex == 3) { topAnimeSelectionFillInformation(warpPanel_TopThisSeason, "movie"); }
            else if (topAnimeSelection.SelectedIndex == 4) { topAnimeSelectionFillInformation(warpPanel_TopThisSeason, "bypopularity"); }
            else if (topAnimeSelection.SelectedIndex == 5) { topAnimeSelectionFillInformation(warpPanel_TopThisSeason, "favorite"); }
        } //top of what category
        #endregion

        private void imageScheduleLeave(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            DoubleAnimation ani = new DoubleAnimation(1, TimeSpan.FromSeconds(0.1));
            img.BeginAnimation(Image.OpacityProperty, ani);
        } //image fade in
        private void imageScheduleEnter(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            DoubleAnimation ani = new DoubleAnimation(0.2, TimeSpan.FromSeconds(0.1));
            img.BeginAnimation(Image.OpacityProperty, ani);
        } //image fade out

        private void imageClick(object sender, RoutedEventArgs e)
        {
            mainTab.SelectedIndex = 6;
            setPreview((sender as Image).Name.Remove(0, 6));
            descriptionTab.SelectedIndex = 0;
            previewTab.Visibility = Visibility.Visible;
        } //preview anime when clicked on picture
        private void setColorOfRadioButtonSeasons(RadioButton rb)
        {
            titleThisSeason.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");
            titleBrowseSeasons.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");
            titleTopThisSeason.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueDarkBrush");

            rb.SetResourceReference(RadioButton.BackgroundProperty, "PrimaryHueMidBrush");
        } //radio button color when clicked on/off
        private void addStatusToSeasonItem(string text, StackPanel sp, int width, int fontSize)
        {
            if (!string.IsNullOrEmpty(filename))
            {
                lines = File.ReadAllLines(filename);
                for (int x = 0; x < lines.Length; x++)
                {
                    string[] words = lines[x].Split(' ');
                    if (words[1].Replace("%20", " ") == text)
                    {
                        TextBlock tbStatus = new TextBlock
                        {
                            FontWeight = FontWeights.Bold,
                            FontSize = fontSize,
                            Foreground = Brushes.White,
                            Width = width,
                            TextWrapping = TextWrapping.Wrap,
                            Padding = new Thickness(0, 3, 0, 5),
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Bottom
                        };

                        if (words[4] == "CurrentlyWatching") { tbStatus.Text = "WATCHING"; tbStatus.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0, 168, 28)); }
                        else if (words[4] == "Completed") { tbStatus.Text = "COMPLETED"; tbStatus.Background = new SolidColorBrush(Color.FromArgb(0xFF, 9, 98, 145)); }
                        else if (words[4] == "OnHold") { tbStatus.Text = "ON-HOLD"; tbStatus.Background = new SolidColorBrush(Color.FromArgb(0xFF, 173, 171, 2)); }
                        else if (words[4] == "Dropped") { tbStatus.Text = "DROPPED"; tbStatus.Background = new SolidColorBrush(Color.FromArgb(0xFF, 235, 64, 52)); }
                        else if (words[4] == "PlantoWatch") { tbStatus.Text = "PLAN TO WATCH"; tbStatus.Background = new SolidColorBrush(Color.FromArgb(0xFF, 84, 84, 84)); }

                        sp.Children.Add(tbStatus);
                    }
                }
            }
        } //check if anime is in your list
        #endregion
    }
}