using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{


    public class MainWindowViewModel
    {
        private readonly Dispatcher _dispatcher;
        public MainWindowViewModel(ObservableCollection<FavShowData> shows)
        {

            _dispatcher = Dispatcher.CurrentDispatcher;
            _shows = shows;
            shows.CollectionChanged += update_source;
            foreach (FavShowData favShowData in shows)
            {

                UpdateCategoriesForShow(favShowData,favShowData.Categories.ToList());
         
                favShowData.Categories.CollectionChanged +=
                    delegate
                    {
                        _dispatcher.Invoke(
                            delegate
                            {
                                UpdateCategoriesForShow(favShowData, favShowData.Categories.ToList());
                            });
                    };   
            }

            Settings.CategorySettings.CollectionChanged += CategorySettings_CollectionChanged;
            foreach (var categorySetting in Settings.CategorySettings)
            {
                categorySetting.PropertyChanged += CategorySetting_PropertyChanged;
            }
        }

        private void CategorySetting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var sett = sender as ShowCategorySetting;
            Debug.Assert(sett != null, "sett != null");
            foreach (FavShowData favShowData in _shows)
            {
                UpdateCategoriesForShow(favShowData, favShowData.Categories.ToList());
            }
        }

        private void CategorySettings_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _categories.Sort(CategoryComparer);
        }

        private readonly ObservableCollection<ShowCategory> _categories = new ObservableCollection<ShowCategory>();

         private static readonly Comparer<ShowCategory> CategoryComparer = Comparer<ShowCategory>.Create((c1, c2) =>
         {
             var settings = Settings.Instance.CategorySettings;
             int cind1 = settings.IndexOf(c1.Setting);
             int cind2 = settings.IndexOf(c2.Setting);
             if (cind1 > cind2) return 1;
             return -1;
         });

        private readonly ObservableCollection<FavShowData> _shows;


        private void update_source(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var newItem in e.NewItems)
                    {
                        var n = (newItem as FavShowData);
                        n.Categories.CollectionChanged += delegate
                        {
                            _dispatcher.Invoke(
                                delegate { UpdateCategoriesForShow(n, n.Categories.ToList()); });
                        };

                        _dispatcher.Invoke(delegate {
                               UpdateCategoriesForShow(n, n.Categories.ToList());
                        });
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in e.OldItems)
                    {
                        var o = oldItem as FavShowData;
                        _dispatcher.Invoke(delegate {
                            //Remove show from cats
                            foreach (var cat in _categories)
                            {
                                var vm = cat.Shows.FirstOrDefault(v => v.Show == o);
                                if ( vm != null)
                                {
                                    cat.Shows.Remove(vm);
                                }
                            }
                        });
                    }
                    //Remove empty cats
                    _dispatcher.Invoke(delegate
                    {
                        _categories.Where(c => !c.Shows.Any()).ToList() .ForEach(c => _categories.Remove(c));
                    });
                    break;
                default:
                    throw new InvalidOperationException(e.Action.ToString());

            }
        }


        private void UpdateCategoriesForShow(FavShowData show,List<String> categories)
        {

            categories.Add("all");
            
            //Check existing cats
            foreach (var cat in _categories)
            {
                if(!cat.Setting.Enabled) continue;
         
                var vm = cat.Shows.FirstOrDefault(v => v.Show == show);
                if (!categories.Contains(cat.Title) && vm!=null ) //should not be there, but is there
                {
                    cat.RemoveShow(vm);
                } else if (categories.Contains(cat.Title) && vm == null) //should be there, but isn't
                {
                    cat.AddShow(new ShowTileViewModel(show));
                }
                else
                {
                    cat.Sort();
                }
            }

            //Remove empty cats or disabled cats
            _categories.Where(c => !c.Shows.Any() || !c.Setting.Enabled).ToList().ForEach(c=> _categories.Remove(c));


            //Add non existing cats
            foreach (var cat in categories)
            {
                if (_categories.Any(c => c.Title == cat)) continue;
                var settings = Settings.Instance.CategorySettings.First(s => s.Title == cat);
                if(!settings.Enabled) continue;
                var newCat = new ShowCategory();
                newCat.Title = cat;
                newCat.Setting = settings;
                newCat.AddShow(new ShowTileViewModel(show));

                _categories.Add(newCat);
            }

            _categories.Sort(CategoryComparer);
               
        }

        public ObservableCollection<ShowCategory> Categories => _categories;
        public Settings Settings => Settings.Instance;

    }
}
