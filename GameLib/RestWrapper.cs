using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameLib
{


  /// <summary>
  /// Rather than simple pass back the results of a API call directly in the body of the response, they
  /// are wrapped in a class that allows us to pass back metadata about the call. In cases of error, a
  /// list of erros can be returned, and in cases of success, other interesting data can be also
  /// returned.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  [DataContract]
  public class RestWrapper<T>
  {
       
    public RestWrapper(T wrappedData)
    {
      Data = wrappedData;
      ExecutionDate = DateTime.UtcNow;
    }

  //  public string Version { get; set; }

  //  public string RequestId { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public int StatusCode { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string ErrorMessage { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public string InfoMessage { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public List<Link> Links { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]  
    public T Data { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] 
    public DateTime ExecutionDate { get; set; }

    public void AddLink(string name, string url)
    {
      if(Links == null)
      {
        Links = new List<Link>();
      }
      Link l = new Link();
      l.Name = name;
      l.URL = url;
      Links.Add(l);
    }
  }


  public class Link
  {
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "rel")]        
    public string Name { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "href")]
    public string URL { get; set; }
  }

}
