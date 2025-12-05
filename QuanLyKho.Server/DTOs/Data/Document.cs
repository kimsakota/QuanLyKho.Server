
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System
{
    using BsonData;
    public class DocumentMap<T> : Dictionary<string, T>
        where T: Document
    {
        new public T this[string objectId]
        {
            get
            {
                if (string.IsNullOrEmpty(objectId))
                {
                    return default(T);
                }

                T value;
                TryGetValue(objectId, out value);

                return value;
            }
            set
            {
                if (base.ContainsKey(objectId))
                {
                    base[objectId] = value;
                }
                else
                {
                    base.Add(objectId, value);
                }
            }
        }
        public void Add(T doc)
        {
            this[doc.ObjectId] = doc;
        }
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var doc in items)
            {
                this.Add(doc);
            }
        }
        new public DocumentMap<T> Clear()
        {
            base.Clear();
            return this;
        }
    }
    public class DocumentMap : DocumentMap<Document> { }
    public class DocumentGroup : Document
    {
        DocumentList records;
        public DocumentList Records
        {
            get
            {
                if (records == null)
                {
                    Push("items", records = new DocumentList());
                }
                return records;
            }
        }
    }
}

namespace System
{
    public class DocumentList<T> : DocumentList
        where T: Document, new()
    {
        protected Func<string, T> find_one;
        public DocumentList(BsonData.Collection table)
        {
            find_one = (id) => table.Find<T>(id);
        }
        public T Add(string id)
        {
            var e = find_one(id);
            if (e != null) base.Add(e);

            return e;
        }
        public void Each(Action<T> callback)
        {
            foreach (var e in this) callback((T)e);
        }
    }
}
