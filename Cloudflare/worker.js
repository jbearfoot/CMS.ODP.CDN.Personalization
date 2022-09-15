addEventListener('fetch', event => {
    event.respondWith(handleRequest(event.request))
  })
  
  // Version of the script, for debug
  const scriptVersion = 1;
  
  async function handleRequest(request) {
    try
    {
      // Get content from CDN or origin    
      let response = await fetch(request);
      let jsonResponse = new Response(response.body, response);
  
      let userFromCookie = getCookie(request.headers.get("Cookie"), "x-opti-user");
      if (!userFromCookie)
      {
          userFromCookie = createRandomUser();
          await CreateUser(userFromCookie);
          const newCookie = "x-opti-user=" + userFromCookie + "; path=/; secure; HttpOnly; SameSite=Strict";
          jsonResponse.headers.append("Set-Cookie", newCookie);
      }
  
      //If response has categories we track usage
      let mainJson = await JSON.parse(await response.text());
      let categories = mainJson["category"];
      if (categories)
      {
          for (let i = 0; i < categories.length; i++)
          {
            await TrackCategory(userFromCookie, categories[i]["name"]);
          }
      }
  
      //Check if there are any personalized properties
      let personalizedProperties = mainJson["personalizedVariants"];
      if (personalizedProperties)
      {
         var segments = await GetMatchedSegments(userFromCookie, GetUsedSegments(personalizedProperties));
         if (segments.length > 0)
         {
            //There are personalization for some user matching segment, swap content
            for (let i = 0; i < personalizedProperties.length; i++)
            {
                let property = personalizedProperties[i];
                for (let j = 0; j < property.variants.length; j++)
                {
                    if (segments.includes(property.variants[j]["name"]))
                    {
                      mainJson = await PersonalizeProperty(property["property"], property.variants[j]["link"], mainJson);
                    }
                }
            }
         }  
      }
   
      return new Response(JSON.stringify(mainJson), jsonResponse);
    }
    catch(err)
    {
      return new Response('Script version: ' + scriptVersion + ', error message: ' + err);
    }
  }
  
  async function TrackCategory(user, category)
  {
      let json = await JSON.parse("[{\"type\": \"event\",\"identifiers\": {\"vuid\": \"\"},\"data\": {\"cms_category\": \"\"}}]");
      json[0]["identifiers"]["vuid"] = user;
      json[0]["data"]["cms_category"] = category;
  
      const init = {
        body: JSON.stringify(json),
        method: 'POST',
        headers: {
          'content-type': 'application/json;charset=UTF-8',
          'x-api-key': ODP_KEY
        },
      };
  
      const response = await fetch(ODP_URI + '/events', init);
      console.log('category status' + response.status);
  }
  
  function GetUsedSegments(personalizedProperties)
  {
      let usedSegments = [];
      for (let i = 0; i < personalizedProperties.length; i++)
      {
          for (let j = 0; j < personalizedProperties[i].variants.length; j++)
          {
              var segment = personalizedProperties[i].variants[j]["name"];
              if (!usedSegments.includes(segment))
              {
                usedSegments.push(segment);
              }
          }
      }
  
      return usedSegments;
  }
  
  async function GetMatchedSegments(user, usedSegments)
  {
      let query = "{\"query\": \"query {customer(vuid: \\\"" + user + "\\\") {audiences(subset: [";
      for (let i = 0; i < usedSegments.length; i++)
      {
         query = query + "\\\"" + usedSegments[i] + "\\\"";
         if (i < usedSegments.length - 1)
         {
           query = query + ",";
         }
      }
      query = query + "]) {edges {node {name}}}}}\"}";
      console.log(query);
  
      const init = {
        body: query,
        method: 'POST',
        headers: {
          'content-type': 'application/json;charset=UTF-8',
          'x-api-key': ODP_KEY
        },
      };
  
      const response = await fetch(ODP_URI + '/graphql', init);
      console.log('segment status' + response.status);
  
      var json = await JSON.parse(await response.text());
      var edges = json["data"]["customer"]["audiences"]["edges"];
      let segments = [];
      for (let i = 0; i < edges.length; i++)
      {
         segments[i] = edges[i]["node"]["name"];
      }
      return segments; 
  }
  
  async function CreateUser(user)
  {
      let json = await JSON.parse("{\"attributes\": {\"vuid\": \"\", \"email\": \"\"}}");
      json["attributes"]["vuid"] = user;
      json["attributes"]["email"] = user + "@test.com";
  
      const init = {
        body: JSON.stringify(json),
        method: 'POST',
        headers: {
          'content-type': 'application/json;charset=UTF-8',
          'x-api-key': ODP_KEY
        },
      };
  
      const response = await fetch(ODP_URI + '/profiles', init);
      console.log('profiles status' + response.status);
  }
  
  async function PersonalizeProperty(propertyName, personalizedLink, mainJson)
  {
    console.log('Personalize property' + propertyName);
    const response = await fetch(personalizedLink);
    var camelCaseName = propertyName[0].toLowerCase() + propertyName.slice(1);
    mainJson[camelCaseName] = await JSON.parse(await response.text());
    return mainJson;
  }
  
  function getCookie(cookieString, key) {
    if (cookieString) {
      const allCookies = cookieString.split("; ")
      const targetCookie = allCookies.find(cookie => cookie.includes(key))
      if (targetCookie) {
        const [_, value] = targetCookie.split("=")
        return value
      }
    }
    return null
  }
  
  function createRandomUser()
  {
    return Math.random().toString(36).slice(2, 10);
  }
  
  
  
  
  