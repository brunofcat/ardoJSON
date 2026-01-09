using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Ardo.ArdoJSON
{
    public class ArdoJSON : IArdoJSON
    {

        public string JSON_Listify(string JSONIn, string? Path = "")
        {

            string[] path = Array.Empty<string>();
            if (Path != null && Path.Length > 0)
            {
                path = Path.Trim().Split('.');
            }
            JToken root = Inner_Listify(JToken.Parse(JSONIn), path, 0);

            StringBuilder sb = new StringBuilder();

            using (JsonWriter json = new JsonTextWriter(new StringWriter(sb)))
            {
                json.Formatting = Formatting.None;
                json.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                root.WriteTo(json);
            }

            return sb.ToString();
        }

        public string JSON_Objectify(string JSONIn, string? Path = "")
        {
            string[] path = Array.Empty<string>();
            if (Path != null && Path.Length > 0)
            {
                path = Path.Trim().Split('.');
            }
            JToken root = Inner_Objectify(JToken.Parse(JSONIn), path, 0);

            StringBuilder sb = new StringBuilder();

            using (JsonWriter json = new JsonTextWriter(new StringWriter(sb)))
            {
                json.Formatting = Formatting.None;
                json.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                root.WriteTo(json);
            }

            return sb.ToString(); ;
        }

        public string DiffJsonLists(string leftJson, string rightJson, string keyField)
        {
            var diffs = new List<object>();
        
            var leftArray = JArray.Parse(leftJson);
            var rightArray = JArray.Parse(rightJson);
        
            var leftMap = leftArray
                .Where(x => x[keyField] != null)
                .ToDictionary(x => x[keyField]!.ToString(), x => x);
        
            var rightMap = rightArray
                .Where(x => x[keyField] != null)
                .ToDictionary(x => x[keyField]!.ToString(), x => x);
        
            // Removed + Changed
            foreach (var kv in leftMap)
            {
                if (!rightMap.ContainsKey(kv.Key))
                {
                    diffs.Add(new
                    {
                        Type = "Removed",
                        Key = kv.Key,
                        OldValue = kv.Value
                    });
                    continue;
                }
        
                var leftObj = kv.Value;
                var rightObj = rightMap[kv.Key];
        
                foreach (var prop in leftObj.Properties())
                {
                    var rProp = rightObj[prop.Name];
                    if (rProp == null || !JToken.DeepEquals(prop.Value, rProp))
                    {
                        diffs.Add(new
                        {
                            Type = "Changed",
                            Key = kv.Key,
                            Field = prop.Name,
                            OldValue = prop.Value,
                            NewValue = rProp
                        });
                    }
                }
            }
        
            // Added
            foreach (var kv in rightMap)
            {
                if (!leftMap.ContainsKey(kv.Key))
                {
                    diffs.Add(new
                    {
                        Type = "Added",
                        Key = kv.Key,
                        NewValue = kv.Value
                    });
                }
            }
        
            return JArray.FromObject(diffs).ToString();
        }
        
        public string JSONSelect(string JSONIn, string? Path = "", bool? AlwaysReturnAsJSON = false)
        {
            var o = JToken.Parse(JSONIn);
            var selEnum = o.SelectTokens(Path?? "");

            bool returnJson = AlwaysReturnAsJSON ?? false;

            string result;
            if (selEnum.Count() == 1)
            {

                result = JsonConvert.SerializeObject(selEnum.ElementAt(0));
                if (!returnJson && selEnum.First().Children().Count() == 0)
                {
                    result = result.Trim('"');
                }
            }
            else
            {
                result = JsonConvert.SerializeObject(selEnum);
            }

            return result;
        }

        private JToken Inner_Objectify(JToken root, string[] path, int index)
        {
            if (path.Length == index)
            {
                // nothing to do if we're not at an array
                if (root.Type != JTokenType.Array)
                    return root;

                // do the objectification
                JArray arr = (JArray)root;
                JObject res = new JObject();
                foreach (JToken p in arr)
                {
                    JObject curr = (JObject)p;

                    res.Add(curr["key"]!.ToString(), curr["value"]);

                }
                return res;
            }
            else
            {
                if (path[index].Equals(""))
                    return Inner_Objectify(root, path, index + 1);

                // path[index] != ""
                if (root.Type == JTokenType.Object)
                {
                    JObject obj = (JObject)root;
                    JToken r = obj[path[index]]!;

                    if (r == null || r.Type == JTokenType.Null)
                        return root;

                    obj[path[index]] = Inner_Objectify(r, path, index + 1);
                    return root;
                }

                if (root.Type == JTokenType.Array)
                {
                    // if we're at an array, simply apply to all elements
                    JArray arr = (JArray)root;
                    for (int i = 0; i < arr.Count; i++)
                    {
                        arr[i] = Inner_Objectify(arr[i], path, index);
                    }
                    return arr;
                }

                return root;
            }
        }
        private JToken Inner_Listify(JToken root, string[] path, int index)
        {

            if (root.Type == JTokenType.Array)
            {
                // if we're at an array, simply apply to all elements
                JArray arr = (JArray)root;
                for (int i = 0; i < arr.Count; i++)
                {
                    arr[i] = Inner_Listify(arr[i], path, index);
                }
                return arr;
            }

            if (path.Length == index)
            {
                // nothing to do if we're not at an object
                if (root.Type != JTokenType.Object)
                    return root;

                // do the listification
                JObject obj = (JObject)root;
                JArray res = new JArray();
                foreach (JProperty p in obj.Properties())
                {
                    JObject tmp = new JObject();
                    tmp["key"] = p.Name;
                    tmp["value"] = p.Value;
                    res.Add(tmp);
                }
                return res;

            }
            else
            {
                if (path[index].Equals(""))
                    return Inner_Listify(root, path, index + 1);

                // path[index] != ""
                if (root.Type == JTokenType.Object)
                {
                    JObject obj = (JObject)root;
                    JToken r = obj[path[index]]!;

                    if (r == null || r.Type == JTokenType.Null)
                        return root;

                    obj[path[index]] = Inner_Listify(r, path, index + 1);
                    return root;
                }

                return root;
            }
        }
    }

}
