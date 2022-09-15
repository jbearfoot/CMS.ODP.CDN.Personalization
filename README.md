I spent last hackdays to look at ODP real time segments. My idea was to use it for personalization of CMS content. So, the steps I took was:
1.	I created a new event in ODP called ‘cms_category’ which I planned to call/track when a user visited content that was categorized in CMS.
2.	I then for each category in CMS created a corresponding real time segment in ODP. They were created so that if a user had seen content categorized with a certain category in CMS then they should match the corresponding segment.
3.	Then I changed CMS so that it when an editor personalize content it fetches and displays segments from OPD instead.
4.	Next step was to extend the Json response from ContentDeliveryAPI so it includes a list of personalized properties including information on which segment it is personalized for and a link to retrieve personalized content for that segment.
5.	Next was to create a Cloudflare worker. My goal was that all personalization should happen on the edge. So, I implemented a worker that does the following for a request for CMS.

5.1.	It first requests the response from CMS or CDN cache (this will be default unpersonalized content).

5.2.	It checks if user has a cookie ‘x-opti-user‘, if not it creates a random identifier for the user. It then creates a profile in ODP for that random user and assigns the ‘x-opti-user’ to the response (so sub-sequent request for same client is identified as same user).

5.3.	It then checks if the content in the response is categorized in cms, if so it tracks/calls the event in ODP for the user.

5.4.	Next it checks if the content contains any personalized properties. If so it then calls ODP to check if user is part of any of the segments the content is personalized for.

5.5.	If the user is part of any personalized segment it then fetches the personalized content for that segment (from CMS or CDN if cached).

5.6.	It then replaces the default content with personalized content and then sends the now personalized content to the user.

Some notes:

•	Since my frontend skills are limited, in step 3, I did not change anything in the CMS UI instead I replace the repository it uses to fetch Visitor groups from with a custom implementation that instead fetched segments from ODP.

•	I have not done any performance tests, but my idea was that content can be cached on CDN (both default un-personalized content and separately personalized properties). So personalization can happen on edge without need to call CMS. The tracking and segment checks against ODP are likely fast so hopefully should the personalization not add to much overhead.

