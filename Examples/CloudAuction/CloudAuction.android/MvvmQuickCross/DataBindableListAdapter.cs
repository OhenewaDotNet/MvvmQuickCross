using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Android.Views;
using Android.Widget;
using System.Collections.Specialized;

namespace MvvmQuickCross
{
    public interface IDataBindableListAdapter
    {
        int GetItemPosition(object item);
        object GetItemAsObject(int position);
        void SetList(IList list);
        void AddHandlers();
        void RemoveHandlers();
    }

    public class DataBindableToStringListAdapter<T> : DataBindableListAdapter<T>
    {
        public DataBindableToStringListAdapter(LayoutInflater layoutInflater, int viewResourceId, int objectValueResourceId, string idPrefix = null)
            : base(layoutInflater, viewResourceId, objectValueResourceId, idPrefix)
        { }

        protected override void UpdateView(View view, T value)
        {
            ViewDataBindings.UpdateView(view, value.ToString());
        }
    }

    public class DataBindableListAdapter<T> : BaseAdapter, IDataBindableListAdapter
    {
        private class ItemDataBinding
        {
            public readonly PropertyInfo ObjectPropertyInfo;
            public readonly FieldInfo ObjectFieldInfo;
            public readonly int ResourceId;

            public string Name { get { return (ObjectPropertyInfo != null) ? ObjectPropertyInfo.Name : ObjectFieldInfo.Name; } }
            public object GetValue(object item) { return (ObjectPropertyInfo != null) ? ObjectPropertyInfo.GetValue(item) : ObjectFieldInfo.GetValue(item); }

            public ItemDataBinding(PropertyInfo objectPropertyInfo, int resourceId)
            {
                this.ObjectPropertyInfo = objectPropertyInfo;
                this.ResourceId = resourceId;
            }

            public ItemDataBinding(FieldInfo objectFieldInfo, int resourceId)
            {
                this.ObjectFieldInfo = objectFieldInfo;
                this.ResourceId = resourceId;
            }
        }

        private readonly LayoutInflater layoutInflater;
        private readonly int viewResourceId;
        private IList list;
        private readonly Type resourceIdType;
        private readonly int? objectValueResourceId;
        private readonly string idPrefix;
        private readonly List<ItemDataBinding> itemDataBindings;

        private DataBindableListAdapter(LayoutInflater layoutInflater, int viewResourceId, Type resourceIdType = null, int? objectValueResourceId = null, string idPrefix = null)
        {
            this.layoutInflater = layoutInflater;
            this.viewResourceId = viewResourceId;
            this.resourceIdType = resourceIdType;
            this.objectValueResourceId = objectValueResourceId;
            this.idPrefix = idPrefix ?? this.GetType().Name + "_";

            if (!objectValueResourceId.HasValue)
            {
                itemDataBindings = new List<ItemDataBinding>();

                foreach (var pi in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var resourceId = ResourceId(IdName(pi.Name));
                    if (resourceId.HasValue) itemDataBindings.Add(new ItemDataBinding(pi, resourceId.Value));
                }

                foreach (var fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var resourceId = ResourceId(IdName(fi.Name));
                    if (resourceId.HasValue) itemDataBindings.Add(new ItemDataBinding(fi, resourceId.Value));
                }
            }
        }

        public DataBindableListAdapter(LayoutInflater layoutInflater, int viewResourceId, int objectValueResourceId, string idPrefix = null)
            : this(layoutInflater, viewResourceId, null, objectValueResourceId, idPrefix)
        { }

        public DataBindableListAdapter(LayoutInflater layoutInflater, int viewResourceId, Type resourceIdType, string idPrefix = null)
            : this(layoutInflater, viewResourceId, resourceIdType, null, idPrefix)
        { }

        private void AddListHandler()
        {
            if (list is INotifyCollectionChanged)
            {
                ((INotifyCollectionChanged)list).CollectionChanged += DataBindableListAdapter_CollectionChanged;
            }
        }

        private void RemoveListHandler()
        {
            if (list is INotifyCollectionChanged)
            {
                ((INotifyCollectionChanged)list).CollectionChanged -= DataBindableListAdapter_CollectionChanged;
            }
        }

        public virtual void AddHandlers() { AddListHandler(); }

        public virtual void RemoveHandlers() { RemoveListHandler(); }

        void DataBindableListAdapter_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // TODO: Check if this should & can be optimized, see for details documentation at http://blog.stephencleary.com/2009/07/interpreting-notifycollectionchangedeve.html
            NotifyDataSetChanged();
        }

        public int GetItemPosition(object item)
        {
            return (list == null) ? -1 : list.IndexOf(item);
        }

        public object GetItemAsObject(int position)
        {
            return (list == null) ? null : list[position];
        }

        public void SetList(IList list)
        {
            RemoveListHandler();
            this.list = list;
            AddListHandler();
            NotifyDataSetChanged();
        }

        public override int Count
        {
            get { return (list == null) ? 0 : list.Count; }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return position; // Bogus implementation required by BaseAdapter - nobody wants a Java object.
        }

        public override long GetItemId(int position)
        {
            return position; // Bogus implementation required by BaseAdapter - Id adds nothing to position
        }

        private string IdName(string name) { return idPrefix + name; }

        private int? ResourceId(string resourceName)
        {
            var fieldInfo = resourceIdType.GetField(IdName(resourceName));
            if (fieldInfo == null) return null;
            return (int)fieldInfo.GetValue(null);
        }

        protected virtual void UpdateView(View view, T value)
        {
            ViewDataBindings.UpdateView(view, value);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var rootView = convertView ?? layoutInflater.Inflate(viewResourceId, parent, false);
            if (list != null)
            {
                if (objectValueResourceId.HasValue)
                {
                    UpdateView(rootView.FindViewById(objectValueResourceId.Value), (T)list[position]);
                }
                else
                {
                    foreach (var idb in itemDataBindings) UpdateView(rootView.FindViewById(idb.ResourceId), (T)idb.GetValue(list[position]));
                }
            }
            return rootView;
        }
    }
}