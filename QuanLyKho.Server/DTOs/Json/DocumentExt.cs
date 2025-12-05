using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    partial class Document
    {
        public string Name { get => GetString("name"); set => Push("name", value); }
        public Document ValueContext => SelectContext("value", v => { });
        public DocumentList ValueList => GetDocumentList("value");
        public DocumentList Items { get => GetDocumentList("items"); set => Push("items", value); }
        public List<string> Fields { get => GetArray<string>("fields"); set => Push("fields", value); }
        public string Url { get { return GetString("url"); } set => Push("url", value); }
        public string Action { get { return GetString("action"); } set => Push("action", value); }

        public object Value
        {
            get
            {
                TryGetValue("value", out object v);
                return v;
            }
            set => Push("value", value);
        }
        public int Code { get { return GetValue<int>("code"); } set => Push("code", value); }
        public string Message { get { return GetString("message"); } set => Push("message", value); }
        public string Role { get => GetString("role"); set => Push("role", value); }
    }
}

namespace System
{
    partial class Document
    {
        public string UserName { get => GetString("userName"); set => Push("userName", value); }
        public string Password { get => GetString("password"); set => Push("password", value); }
    }
}

namespace System
{
    public class DocumentList : List<Document>
    {
        public DocumentList() { }
        public DocumentList(IEnumerable<Document> items)
        {
            this.AddRange(items);
        }
        public Document Push(object value)
        {

            Document doc = value as Document;
            if (doc == null)
            {
                doc = JObject.ToDocument(value);
            }
            base.Add(doc);

            return doc;
        }
        public string Join(string seperator, string fieldName)
        {
            var s = "";
            foreach (var e in this)
            {
                var a = e.GetString(fieldName);
                if (s.Length > 0) s += seperator;

                s += a;
            }
            return s;
        }

        public void ForEach<T>(Action<T> callback)
            where T: Document
        {
            foreach (T e in this)
            {
                callback(e);
            }
        }
        public DocumentList Convert<T>() where T : Document, new()
        {
            var lst = new DocumentList();
            foreach (var e in this)
            {
                Document a = e;
                if (!(e is T)) a = e.ChangeType<T>();
                lst.Add(a);
            }
            return lst;
        }

        public DocumentGroup[] GroupBy(params string[] names)
        {
            var map = new Dictionary<string, DocumentGroup>();
            foreach (var doc in this)
            {
                DocumentGroup ext;
                var key = doc.Unique(names);

                if (!map.TryGetValue(key, out ext))
                {
                    map.Add(key, ext = new DocumentGroup());
                    ext.Copy(doc, names);
                }
                ext.Records.Add(doc);
            }

            return new List<DocumentGroup>(map.Values).ToArray();
        }
        public Dictionary<string, Document> ToDictionary(string key)
        {
            var map = new Dictionary<string, Document>();
            foreach (var e in this)
            {
                var k = key == null ? e.ObjectId : e.GetString(key);
                if (k != null) map.Add(k, e);
            }
            return map;
        }
    }
}
