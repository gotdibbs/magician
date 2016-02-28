using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Magician.WebResourceLiveSync.Model
{
    public class DirectoryItem : ViewModelBase
    {
        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { Set(ref _isExpanded, value); }
        }

        private bool _isFile;
        public bool IsFile
        {
            get { return _isFile; }
            set { Set(ref _isFile, value); }
        }

        private bool _isFolder;
        public bool IsFolder
        {
            get { return _isFolder; }
            set { Set(ref _isFolder, value); }
        }

        private bool _isUpToDate;
        public bool IsUpToDate
        {
            get { return _isUpToDate; }
            set
            {
                Set(ref _isUpToDate, value);

                RaisePropertyChanged(() => IsStateUnknown);
            }
        }

        private bool _isOutOfDate;
        public bool IsOutOfDate
        {
            get { return _isOutOfDate; }
            set
            {
                Set(ref _isOutOfDate, value);

                RaisePropertyChanged(() => IsStateUnknown);
            }
        }

        private bool _isSynching;
        public bool IsSynching
        {
            get { return _isSynching; }
            set
            {
                Set(ref _isSynching, value);

                RaisePropertyChanged(() => IsStateUnknown);
            }
        }

        public bool IsStateUnknown
        {
            get
            {
                return !(IsUpToDate || IsOutOfDate || IsSynching);
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;

                Extension = Path.GetExtension(_name).ToLowerInvariant();
            }
        }

        private string _extension;
        public string Extension
        {
            get { return _extension; }
            private set { Set(ref _extension, value); }
        }

        private Uri _fullname;
        public Uri FullName
        {
            get { return _fullname; }
            set { Set(ref _fullname, value); }
        }

        private ObservableCollection<DirectoryItem> _items;
        public ObservableCollection<DirectoryItem> Items
        {
            get { return _items; }
            set { Set(ref _items, value); }
        }

        public DirectoryItem()
        {
            Items = new ObservableCollection<DirectoryItem>();
        }
    }
}
