﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;

namespace TestsIntegration
{
  public static class Fixtures
  {
    public static readonly ServerInfo Server = new ServerInfo { url = "http://localhost:3000", name = "Docker Server" };

    public static Account SeedUser()
    {
      var seed = Guid.NewGuid().ToString().ToLower();
      var user = new Dictionary<string, string>();
      user["email"] = $"{seed.Substring(0, 7)}@acme.com";
      user["password"] = "12ABC3456789DEF0GHO";
      user["name"] = $"{seed.Substring(0, 5)} Name";

      var registerRequest = (HttpWebRequest)WebRequest.Create($"{Server.url}/auth/local/register?challenge=challengingchallenge");
      registerRequest.Method = "POST";
      registerRequest.ContentType = "application/json";
      registerRequest.AllowAutoRedirect = false;


      using (var streamWriter = new StreamWriter(registerRequest.GetRequestStream()))
      {
        string json = JsonConvert.SerializeObject(user);
        streamWriter.Write(json);
        streamWriter.Flush();
      }

      WebResponse response;
      string redirectUrl = null;
      try
      {
        response = registerRequest.GetResponse();
        redirectUrl = response.Headers[HttpResponseHeader.Location];
        Debug.WriteLine(redirectUrl);
      }
      catch (WebException e)
      {
        if (e.Message.Contains("302"))
        {
          Console.WriteLine("We are redirected!");
          response = e.Response;
          redirectUrl = e.Response.Headers[HttpResponseHeader.Location];
          Console.WriteLine("We are redirected; but in an error.");
          Console.WriteLine(redirectUrl);
        }
      }

      var tokenRequest = (HttpWebRequest)WebRequest.Create($"{Server.url}/auth/token");
      tokenRequest.Method = "POST";
      tokenRequest.ContentType = "application/json";

      Console.WriteLine(redirectUrl);
      Console.WriteLine("Why do the tests pass locally?");
      var accessCode = redirectUrl.Split("?access_code=")[1];
      var tokenBody = new Dictionary<string, string>()
      {
        ["accessCode"] = accessCode,
        ["appId"] = "spklwebapp",
        ["appSecret"] = "spklwebapp",
        ["challenge"] = "challengingchallenge"
      };

      using (var streamWriter = new StreamWriter(tokenRequest.GetRequestStream()))
      {
        string json = JsonConvert.SerializeObject(tokenBody);
        streamWriter.Write(json);
        streamWriter.Flush();
      }

      var tokenResponse = tokenRequest.GetResponse();
      var deserialised = new Dictionary<string, string>();
      using (var streamReader = new StreamReader(tokenResponse.GetResponseStream()))
      {
        var text = streamReader.ReadToEnd();
        deserialised = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
      }

      var acc = new Account { token = deserialised["token"], userInfo = new UserInfo { id = user["name"], email = user["email"] }, serverInfo = Server };
      var client = new Client(acc);

      var user1 = client.UserGet().Result;
      acc.userInfo.id = user1.id;

      return acc;
    }

    public static Base GenerateSimpleObject()
    {
      var @base = new Base();
      @base["foo"] = "foo";
      @base["bar"] = "bar";
      @base["baz"] = "baz";
      @base["now"] = DateTime.Now.ToString();

      return @base;
    }

    public static Base GenerateNestedObject()
    {
      var @base = new Base();
      @base["foo"] = "foo";
      @base["bar"] = "bar";
      @base["@baz"] = new Base();
      ((Base)@base["@baz"])["mux"] = "mux";
      ((Base)@base["@baz"])["qux"] = "qux";

      return @base;
    }

    public static Blob[] GenerateThreeBlobs()
    {
      // TODO: remove local file dependency
      var blob = new Blob();
      blob.filePath = Path.Combine("/Users/dim/Downloads", "email-header.png");

      var blob2 = new Blob();
      blob2.filePath = Path.Combine("/Users/dim/Downloads", "2015_09.pdf");

      var blob3 = new Blob();
      blob3.filePath = Path.Combine("/Users/dim/Downloads", "comments.gif");
      return new Blob[] {blob, blob2, blob3};
    }
  }

  public class UserIdResponse
  {
    public string userId { get; set; }
    public string apiToken { get; set; }
  }
}
