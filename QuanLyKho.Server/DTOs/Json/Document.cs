using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class JObject
    {
        static public Document ToDocument(object obj)
        {
            if (obj is string s)
            {
                return StringToDocument(s);
            }
            if (obj is Document d)
            {
                return d;
            }

            var doc = new Document();
            if (obj is IDictionary m)
            {
                foreach (var k in m.Keys)
                {
                    var v = m[k];
                    if (v == null || v.Equals(string.Empty)) continue;

                    doc.Add(k.ToString(), v);
                }
                return doc;
            }
            if (obj != null)
            {
                foreach (var p in obj.GetType().GetProperties())
                {
                    var v = p.GetValue(obj);
                    if (v == null || v.Equals(string.Empty)) continue;
                    doc.Add(p.Name, v);
                }
            }
            return doc;
        }
        static public T ToObject<T>(Document doc)
        {
            var t = typeof(T);
            var o = (T)Activator.CreateInstance(t);

            foreach (var p in t.GetProperties())
            {
                if (p.CanWrite && doc.TryGetValue(p.Name, out var v))
                {
                    p.SetValue(o, v);
                }
            }
            return o;
        }
        static public Document StringToDocument(string s)
        {
            var term = Term.Split(s);
            return (Document)term?.GetValue(s) ?? new Document();
        }
        static public List<object> StringToList(string s)
        {
            var term = Term.Split(s);
            return (List<object>)term.GetValue(s);
        }

        static string quot(object o) => $"\"{o}\"";

        static int _ident = -1;
        static object val2str(object o)
        {
            var t = o.GetType();
            if (t == typeof(string))
                return quot(o);

            if (t == typeof(DateTime))
            {
                return quot($"{o:yyyy-MM-dd HH:mm:ss.fff}");
            }

            if (o is IDictionary m)
            {
                return map2str(m);
            }

            if (o is IEnumerable)
            {
                var lst = new List<object>();
                foreach (var e in (IEnumerable)o)
                {
                    lst.Add(val2str(e));
                }
                return "[" + string.Join(_ident < 0 ? "," : ", ", lst) + "]";
            }

            if (t.IsClass)
            {
                return map2str(ToDocument(o));
            }

            return o;
        }
        static string map2str(IDictionary map)
        {
            if (map.Count == 0)
            {
                return "{}";
            }

            var lines = new List<string>();
            foreach (var k in map.Keys)
            {
                var v = map[k];
                if (v == null || v.Equals(string.Empty)) continue;

                lines.Add($"{quot(k)}:{val2str(v)}");
            }
            return "{" + string.Join(",", lines) + "}";
        }

        static public string DocumentToString(Document document) => map2str(document);

        #region TERM
        abstract class Term
        {
            public int Start { get; set; }
            public int End { get; set; }

            static public ScopeTerm create_scope(char c)
            {
                switch (c)
                {
                    case '[': return new ArrayTerm();
                    case '{': return new ObjectTerm();
                }
                return null;
            }

            static protected bool isQuot(char c) => c == '\"' || c == '\'';
            static public Term Split(string input)
            {
                StringTerm quot = null;
                ValueTerm val = null;

                Term last = null;

                int index = 0;
                var s = new Stack<ScopeTerm>();

                Action<Term> push = term => {
                    term.Start = index;
                    s.Push((ScopeTerm)term);
                };

                Func<Term, Term> add = term => {
                    term.End = index;

                    if (s.Count > 0)
                        s.Peek().Append(term);

                    val = null;
                    return term;
                };

                Action pop = () => {
                    add(last = s.Pop());
                };

                for (index = 0; index < input.Length; index++)
                {
                    char c = input[index];
                    
                    last = create_scope(c);
                    if (last != null)
                    {
                        push(last);
                        break;
                    }
                    switch (c)
                    {
                        case '\r':
                        case '\n':
                        case ' ':
                            continue;
                        default:
                            return null;
                    }
                }

                for (++index; index < input.Length; index++)
                {
                    char c = input[index];
                    if (' ' >= c || c == ':')
                    {
                        if (val != null)
                            add(val);
                        continue;
                    }

                    if (quot != null)
                    {
                        if (quot.IsCloseValid(input, c))
                        {
                            add(quot);
                            quot = null;
                        }
                        continue;
                    }
                    if (isQuot(c))
                    {
                        quot = new StringTerm { Start = index };
                        continue;
                    }

                    switch (c)
                    {
                        case '{': push(new ObjectTerm()); break;
                        case '[': push(new ArrayTerm()); break;

                        case '}':
                        case ']':
                            if (s.Peek().IsCloseValid(input, c))
                            {
                                if (val != null)
                                {
                                    add(val);
                                }

                                pop();
                            }
                            break;

                        case ',':
                            if (val != null)
                                add(val);
                            break;

                        default:
                            if (val == null)
                            {
                                val = new ValueTerm { Start = index };
                            }
                            break;
                    }
                }

                return last;
            }

            protected virtual bool IsCloseValid(string input, char c) => true;
            public virtual string GetKey(string input) => (string)GetValue(input);
            public abstract object GetValue(string input);
        }
        abstract class ScopeTerm : Term
        {
            public virtual void Append(Term term)
            {
                if (_values == null)
                    _values = new List<Term>();
                _values.Add(term);
            }
            protected List<Term> _values;
        }
        class ArrayTerm : ScopeTerm
        {
            protected override bool IsCloseValid(string input, char c) => c == ']';
            public override object GetValue(string input)
            {
                var lst = new List<object>();
                if (_values != null)
                {
                    foreach (var term in _values)
                    {
                        lst.Add(term.GetValue(input));
                    }
                }
                return lst;
            }
        }
        class ObjectTerm : ScopeTerm
        {
            protected override bool IsCloseValid(string input, char c) => c == '}';

            List<Term> _keys;
            public override void Append(Term term)
            {
                if (_keys == null)
                {
                    _keys = new List<Term>();
                    _values = new List<Term>();
                }
                if (_keys.Count == _values.Count)
                {
                    _keys.Add(term);
                }
                else
                {
                    _values.Add(term);
                }
            }
            public override object GetValue(string input)
            {
                var doc = new Document();
                if (_keys != null)
                {
                    var keys = new string[_keys.Count];
                    int i = 0;

                    foreach (var t in _keys)
                    {
                        keys[i++] = t.GetKey(input);
                    }

                    i = 0;
                    foreach (var t in _values)
                    {
                        var v = t.GetValue(input);
                        if (v != null)
                        {
                            doc.Add(keys[i++], v);
                        }
                    }
                }
                return doc;
            }
        }
        class StringTerm : Term
        {
            protected override bool IsCloseValid(string input, char c) => c == input[Start];
            public override object GetValue(string input)
            {
                return input.Substring(Start + 1, End - Start - 1);
            }
        }
        class ValueTerm : Term
        {
            public override string GetKey(string input)
            {
                return input.Substring(Start, End - Start).Trim();
            }
            public override object GetValue(string input)
            {
                int i = Start;

                switch (input[Start])
                {
                    case 'n': return null;
                    case 'T':
                    case 't':
                        return true;

                    case 'F':
                    case 'f':
                        return false;

                    case '+':
                    case '-':
                        i++;
                        break;
                }

                long a = 0, b = 0;
                for (; i < End; i++)
                {
                    var c = input[i];
                    if (c == '.') { b = 1; continue; }
                    if (c < '0' || c > '9')
                        break;

                    a = (a << 1) + (a << 3) + (c & 15);
                    if (b > 0)
                    {
                        b = (b << 1) + (b << 3);
                    }
                }

                if (input[Start] == '-') a = -a;
                if (b == 0)
                    return a;

                return (double)a / b;

            }
        }
        #endregion
    }
    public partial class Document : Dictionary<string, object>
    {
        #region CONVERT
        static public Document FromObject(object src) => JObject.ToDocument(src);
        static public Document Parse(string s) => JObject.StringToDocument(s);
        public override string ToString() => JObject.DocumentToString(this);
        #endregion
    }
}

namespace System
{
    public partial class Document
    {
        #region CLONE
        public Document Clone()
        {
            var doc = new Document();
            foreach (var p in this)
            {
                if (p.Value != null && !p.Value.Equals(string.Empty))
                {
                    doc.Add(p.Key, p.Value);
                }
            }
            return doc;
        }
        public Document Copy(Document src) => Copy(src, null);
        public Document Copy(Document src, IEnumerable<string> names)
        {
            if (names == null) names = src.Keys.ToArray();
            foreach (var name in names)
            {

                if (this.ContainsKey(name) == false)
                {
                    object v = null;
                    src.TryGetValue(name, out v);
                    if (v != null)
                    {
                        base.Add(name, v);
                    }
                }
            }
            return this;
        }
        public Document Move(Document dst) => Move(dst, null);
        public Document Move(Document dst, IEnumerable<string> names)
        {
            if (names == null) names = Keys.ToArray();

            foreach (var name in names)
            {
                findField(name, (k, v) => dst.Push(k, v));
            }
            return dst;
        }
        public T ChangeType<T>() where T : Document, new()
        {
            var dst = new T();
            dst.Copy(this);

            return dst;
        }

        public static Document FromList(IEnumerable<Document> src, string name)
        {
            var doc = new Document();
            foreach (var e in src)
            {
                doc.Add(e.ObjectId, e.GetString(name));
            }
            return doc;
        }
        #endregion

        string correctName(string key)
        {
            //if (char.IsUpper(key[0]))
            //    return char.ToLower(key[0]) + key.Substring(1);
            return key;
        }
        void findField(string name, Action<string, object> callback)
        {
            var k = correctName(name);
            if (TryGetValue(k, out object v))
                callback(k, v);
        }
        object getField(string name, Func<string, object, object> callback)
        {
            var k = correctName(name);
            TryGetValue(k, out object v);

            return callback(k, v);
        }

        public void Push(string name, object value)
        {
            var k = correctName(name);
            Remove(k);
            if (value != null && !value.Equals(string.Empty))
            {
                base.Add(k, value);
            }
        }
        public object Pop(string name) => getField(name, (k, v) => { Remove(k); return v; });
        public T Pop<T>(string name) => (T)(Pop(name) ?? default(T));

        /// <summary>
        /// Select a document, if has callback then create document when not found
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public T SelectContext<T>(string name, Action<T> callback) where T : Document, new()
        {
            return (T)getField(name, (k, v) => {
                if (v == null)
                {
                    if (callback == null) return null;
                    Push(k, v = new T());
                }
                else if (v.GetType() != typeof(T))
                {
                    var doc = v is string ? Parse((string)v) : JObject.ToDocument(v);
                    v = doc.ChangeType<T>();

                    Push(k, v);
                }

                var context = (T)v;
                if (callback != null)
                {
                    callback.Invoke(context);
                }
                return context;
            });
        }
        public Document SelectContext(string name, Action<Document> callback) => SelectContext<Document>(name, callback);

        #region GET ITEMS VALUES
        public T GetDocument<T>(string name) where T : Document, new()
        {
            var doc = GetDocument(name);
            return doc == null ? new T() : doc.ChangeType<T>();
        }
        public Document GetDocument(string name)
        {
            return (Document)getField(name, (k, v) => {
                if (v == null)
                {
                    return new Document();
                }
                return FromObject(v);
            });
        }

        public DocumentList GetDocumentList(string name)
        {
            var src = GetArray(name);
            var lst = new DocumentList();

            foreach (var o in src)
            {
                lst.Add(FromObject(o));
            }
            return lst;
        }
        public List<T> GetArray<T>(string name)
        {
            var src = GetArray(name);
            var lst = new List<T>();

            foreach (var e in src)
            {
                if (e is Document doc)
                {
                    lst.Add(JObject.ToObject<T>(doc));
                }
                else
                {
                    lst.Add((T)Convert.ChangeType(e, typeof(T)));
                }
            }
            return lst;
        }
        public List<object> GetArray(string name)
        {
            return (List<object>)getField(name, (k, v) => {
                if (v is List<object>)
                {
                    return v;
                }

                if (v is string s)
                {
                    return JObject.StringToList(s);
                }

                List<object> lst = new List<object>();
                if (v is IEnumerable<object> se)
                {
                    foreach (object e in se)
                        lst.Add(e);
                }
                return lst;
            });
        }
        public T GetValue<T>(string name, T defaultValue)
        {
            return (T)getField(name, (k, v) => {
                if (v != null)
                {
                    try
                    {
                        return Convert.ChangeType(v, typeof(T));
                    }
                    catch
                    {
                    };
                }
                return defaultValue;
            });
        }
        public T GetValue<T>(string name) => GetValue(name, default(T));
        public DateTime? GetDateTime(string name) => GetValue<DateTime?>(name);
        public virtual string GetString(string name) => GetValue<string>(name);
        public object SelectPath(string path)
        {
            var s = path.Split('.');
            var n = s.Length - 1;
            var doc = this;
            for (int i = 0; i < n; i++)
            {
                if (doc.Count == 0)
                    return null;

                doc = doc.GetDocument(s[i]);
            }
            doc.TryGetValue(correctName(s[n]), out object v);
            return v;
        }

        public bool Find(string key, Action<object> callback)
        {
            if (TryGetValue(key, out var v))
            {
                callback?.Invoke(v);
                return true;
            }
            return false;
        }
        #endregion

        #region ObjectId
        public string ObjectId { get => GetString("_id"); set => Push("_id", value); }
        public string Join(string seperator, params string[] names)
        {
            var lst = new List<object>();
            if (names.Length == 0)
            {
                names = Keys.ToArray();
            }
            foreach (string name in names)
            {
                findField(name, (k, v) => lst.Add(v));
            }
            return string.Join(seperator, lst);
        }
        public string Unique(params string[] names) => this.Join("_", names);
        #endregion
    }
}
