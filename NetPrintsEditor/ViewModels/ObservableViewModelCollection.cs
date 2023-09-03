using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;

using NetPrints.Core;

namespace NetPrintsEditor.ViewModels
{
    public class ObservableViewModelCollection<TViewModel, TModel> : ObservableRangeCollection<TViewModel>
    {
        private readonly INotifyCollectionChanged source;
        private readonly Func<TModel, TViewModel> viewModelFactory;

        public ObservableViewModelCollection(ReadOnlyObservableCollection<TModel> source, Func<TModel, TViewModel> viewModelFactory)
            : this((INotifyCollectionChanged)source, viewModelFactory)
        {
            Contract.Requires(source != null);
            Contract.Requires(viewModelFactory != null);
        }

        public ObservableViewModelCollection(ObservableRangeCollection<TModel> source, Func<TModel, TViewModel> viewModelFactory)
            : this((INotifyCollectionChanged)source, viewModelFactory)
        {
            Contract.Requires(source != null);
            Contract.Requires(viewModelFactory != null);
        }

        public ObservableViewModelCollection(INotifyCollectionChanged source, Func<TModel, TViewModel> viewModelFactory)
            : base(((IEnumerable<TModel>)source).Select(model => viewModelFactory(model)))
        {
            Contract.EndContractBlock();

            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            this.source.CollectionChanged += OnSourceCollectionChanged;
        }

        protected virtual TViewModel CreateViewModel(TModel model)
        {
            return viewModelFactory(model);
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddCase();
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count == 1)
                    {
                        Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else
                    {
                        List<TViewModel> items = this.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();
                        for (int i = 0; i < e.OldItems.Count; i++)
                            RemoveAt(e.OldStartingIndex);

                        for (int i = 0; i < items.Count; i++)
                            Insert(e.NewStartingIndex + i, items[i]);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < e.OldItems.Count; i++)
                        RemoveAt(e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // remove
                    for (int i = 0; i < e.OldItems.Count; i++)
                        RemoveAt(e.OldStartingIndex);

                    // add
                    AddCase();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Clear();
                    break;

                default:
                    break;
            }

            void AddCase()
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    Insert(e.NewStartingIndex + i, CreateViewModel((TModel)e.NewItems[i]));
                }
            }
        }
    }
}
