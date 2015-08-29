using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class ShowCategory
    {
        public String Title { get; internal set; }
        public string Description { get; internal set; }
        public ObservableCollection<ShowTileViewModel> Shows { get; } = new ObservableCollection<ShowTileViewModel>();
    }

    public class MainWindowViewModel
    {
        private readonly Dispatcher _dispatcher;
        public MainWindowViewModel(ObservableCollection<FavShowData> shows)
        {

            _dispatcher = Dispatcher.CurrentDispatcher;
            shows.CollectionChanged += update_source;

            foreach (FavShowData favShowData in shows)
            {

                UpdateCategoriesForShow(favShowData,favShowData.Categories.ToList());
                favShowData.PropertyChanged += OnFavShowDataOnPropertyChanged ;
         
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
        }

        private void OnFavShowDataOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(FavShowData.NextEpisodeDate) || args.PropertyName == nameof(FavShowData.PreviousEpisodeDate))
            {
                _dispatcher.Invoke(delegate
                {
                    foreach (var showCategory in _categories)
                    {
                        showCategory.Shows.Sort(CategoryInnerOrders[showCategory.Title]);
                    }
                });
            }
        }


        private readonly ObservableCollection<ShowCategory> _categories = new ObservableCollection<ShowCategory>();

        private static readonly List<String> CategoryOrders = new List<string>(new []{
            "new",
            "update",
            "active",
            "ended",
            "unknown",
            "all"
        }) ;

        private static readonly Dictionary<String, String> CategoryDescriptions = new Dictionary<string, string>()
        {
            {"new","Shows with new Episodes"},
            {"update", "Shows with updated Episodes" },
            {"active","Active Shows"},
            {"ended","Shows that have ended"},
            {"unknown","Shows with unknown state" },
            {"all","All your Tv Shows"}
        };


        private static readonly Comparer<ShowTileViewModel> AlphabeticalShowComparer = Comparer<ShowTileViewModel>.Create((m1, m2) => String.CompareOrdinal(m1.Title, m2.Title));
        private static readonly Comparer<ShowTileViewModel> DateShowComparer = Comparer<ShowTileViewModel>.Create(
            (vm1, vm2) =>
            {
                if (vm1.Show.NextEpisodeDate.HasValue && vm2.Show.NextEpisodeDate.HasValue) //both have next ep date
                {
                    if (vm1.Show.NextEpisodeDate.Value.Date != vm2.Show.NextEpisodeDate.Value.Date)
                    {
                        if (vm1.Show.NextEpisodeDate.Value.Date < vm2.Show.NextEpisodeDate.Value.Date)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                       
                } else if (vm1.Show.NextEpisodeDate.HasValue)
                {
                    return -1;
                } else if (vm2.Show.NextEpisodeDate.HasValue)
                {
                    return 1;
                }


                if (vm1.Show.PreviousEpisodeDate.HasValue && vm2.Show.PreviousEpisodeDate.HasValue) //both have prev ep date
                {
                    if (vm1.Show.PreviousEpisodeDate.Value.Date != vm2.Show.PreviousEpisodeDate.Value.Date)
                    {
                        if (vm1.Show.PreviousEpisodeDate.Value.Date > vm2.Show.PreviousEpisodeDate.Value.Date)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }

                }
                else if (vm1.Show.PreviousEpisodeDate.HasValue)
                {
                    return -1;
                }
                else if (vm2.Show.PreviousEpisodeDate.HasValue)
                {
                    return 1;
                }

                return AlphabeticalShowComparer.Compare(vm1, vm2);

            }); 


        private static readonly  Dictionary<String,Comparer<ShowTileViewModel>> CategoryInnerOrders = new Dictionary<string, Comparer<ShowTileViewModel>>()
        {
            {"new", DateShowComparer },
            {"update", DateShowComparer },
            {"active", DateShowComparer },
            {"ended", DateShowComparer },
            {"unknown", AlphabeticalShowComparer },
            {"all", AlphabeticalShowComparer }

        }; 



        private static readonly Comparer<ShowCategory> CategoryComparer = Comparer<ShowCategory>.Create((c1, c2) =>
        {
            if (c1.Title == c2.Title) return 0;
            foreach (var cat in CategoryOrders)
            {
                if (c1.Title == cat) return -1;
                if (c2.Title == cat) return 1;
            }
            return 0;
        });

  

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
                       n.PropertyChanged += OnFavShowDataOnPropertyChanged;

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
                var vm = cat.Shows.FirstOrDefault(v => v.Show == show);
                if (!categories.Contains(cat.Title) && vm!=null ) //should not be there, but is there
                {
                    cat.Shows.Remove(vm);
                } else if (categories.Contains(cat.Title) && vm == null) //should be there, but isn't
                {
                    cat.Shows.Add(new ShowTileViewModel(show));
                    cat.Shows.Sort(CategoryInnerOrders[cat.Title]);
                }
            }

            //Remove empty cats
            _categories.Where(c => !c.Shows.Any()).ToList().ForEach(c=> _categories.Remove(c));


            //Add non existing cats
            foreach (var cat in categories)
            {
                if (_categories.Any(c => c.Title == cat)) continue;
                var newCat = new ShowCategory();
                newCat.Title = cat;
                newCat.Description = CategoryDescriptions[cat];
                newCat.Shows.Add(new ShowTileViewModel(show));

                _categories.Add(newCat);
            }

            _categories.Sort(CategoryComparer);
               
        }

        public ObservableCollection<ShowCategory> Categories => _categories;
        public Settings Settings => Settings.Instance;

    }
}
