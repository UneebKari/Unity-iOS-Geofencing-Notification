using System;
using System.Collections;
using Unity.Notifications.iOS;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OneSignalSDK;

public class LocationNotificationManager : MonoBehaviour
{
    // =========================NOTE============================ //
    // Required package "com.unity.mobile.notifications": "2.1.1",
    //Text area to show the corordinates and distnace details on canvas
    public TMP_Text locatoinDetails;
    public TMP_Text distanceDetails;
    public TMP_Text notificationDeails;
    public TMP_Text timerDetails;
    //Input field for custom coordinates entry
    public TMP_InputField latitudeInputField, longitudeInputField;
    public TMP_Dropdown locationList;
    //Toggle for custom value change
    public Toggle canUseCustomValues;

    
    float startingPointLatitude, startingPointLongitude;
    //Uneeb Office Location for testing purpose, you can enter your own
    float officeLat = 31.47136f;
    float officeLong = 74.36275f;
    //Uneeb Home Location for testing purpose, you can enter your own
    float homeLat = 31.46896f;
    float homeLong = 74.25484f;

    bool isClickedOnExit = false;
    double distance;

    private void Start()
    {
        OneSignal.Default.Initialize("ab954571-7d3d-4421-81b3-3eabd6150ac8");
        //Status of ability to send push notifications to the current device (See status chart below)
        var currentStatus = OneSignal.Default.NotificationPermission;
        if (currentStatus == NotificationPermission.NotDetermined)
        {
            // do if user was not prompted
        }

        //The unique OneSignal id of this subscription
        //var pushUserId = OneSignal.Default.PushSubscriptionState.userId;

        //The device's push subscription status
        //var pushState = OneSignal.Default.PushSubscriptionState;

        //The unique token provided by the device's operating system used to send push notifications
        //var pushToken = OneSignal.Default.PushSubscriptionState.pushToken;

        //Returns value of pushDisabled method
        //var isPushDisabled = OneSignal.Default.PushSubscriptionState.isPushDisabled;

        OneSignal.Default.EnterLiveActivity("my_activity_id", "ab954571-7d3d-4421-81b3-3eabd6150ac8");
        //setting values for testing scene for custom value entries
        canUseCustomValues.isOn = false;
        //allows app to run in background
        Application.runInBackground = true;

    }
    private void Update()
    {
        if (Input.location.isEnabledByUser)
        {
            //Showing currnet upadted cooridinates
            locatoinDetails.text = "Current latitude is = " + Input.location.lastData.latitude + "\n"
                + "Current longitude is = " + Input.location.lastData.longitude;
            //distanceDetails.text = "Current Distance from Start Point is = " + CalculateDistance();
            CalculateTheDistanceBetweenCoordinatesAndCurrentCoordinates(startingPointLatitude, startingPointLongitude, Input.location.lastData.latitude, Input.location.lastData.longitude);
        }
        timerDetails.text = Time.time.ToString("F0");
    }
    //If initialize location services by function
    public void Init()
    {
        StartCoroutine(RequestAuthorization());
    }
    
    /*This method must be called before you attempt to schedule any local notifications.
    If "Request Authorization on App Launch" is enabled in
    "Edit -> Project Settings -> Mobile Notification Settings"
    this method will be called automatically when the app launches.
    You might call this method again to determine the current authorizations
    status or retrieve the DeviceToken for Push Notifications.
    However the UI system prompt will not be shown if the user has already granted or denied
    authorization for this app. */
    IEnumerator RequestAuthorization()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            };

            string res = "\n RequestAuthorization:";
            res += "\n finished: " + req.IsFinished;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;
            Debug.Log(res);
            //isLocationEnabled = req.Granted;
            if (req.Granted) {
                //It will show the location permission promp if not allowed by the user
                StartCoroutine(ShowPromptForLocationPermission());
            }
        }
    }
    //Force showing promopt for location permission
    public void ReOpenTheLocationPermissionPrompt()
    {
        StartCoroutine(ShowPromptForLocationPermission());
    }
    [System.Obsolete]
    IEnumerator ShowPromptForLocationPermission()
    {
        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
            yield break;

        // Starts the location service.
        Input.location.Start();

        // Waits until the location service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds this cancels location service use.
        if (maxWait < 1)
        {
            print("Timed out");
            yield break;
        }
        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            print("Unable to determine device location");
            yield break;
        }
        else
        {
            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
            print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        }

        //If reqiuired then we can stop the location service by this
        //Input.location.Stop();

    }
    public void StopLocationServices() {
        //If reqiuired then we can stop the location service by this
        Input.location.Stop();
    }
    public void ScheduleNotificationOnEnterInARadius() {
        if (!canUseCustomValues.isOn)
        {
            startingPointLatitude = Input.location.lastData.latitude;
            startingPointLongitude = Input.location.lastData.longitude;
        }

        var enterlocationTrigger = new iOSNotificationLocationTrigger()
        {
            Center = new Vector2(startingPointLatitude, startingPointLongitude),
            Radius = 5f,
            NotifyOnEntry = true,
            NotifyOnExit = false,
        };
        Debug.Log("Push Notification is set for a radius of " + enterlocationTrigger.Radius.ToString() + "Meters"
          + " When user enters in " + "Latitude = "+ Input.location.lastData.latitude + "===" + "Longitude = " + Input.location.lastData.longitude);

        var EntryBasedNotification = new iOSNotification()
        {
            Title = "Exited",
            Subtitle = "You Have Entered in the radius of " + enterlocationTrigger.Center + " Meters",
            Body = "Radius latitude was > " + startingPointLatitude + " and longitude was > " + startingPointLongitude,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            Trigger = enterlocationTrigger
        };
        iOSNotificationCenter.ScheduleNotification(EntryBasedNotification);
        //schedule notification for entry base
        Debug.Log("Started a location notification at " + enterlocationTrigger.Center);


    }
    //[System.Obsolete]
    public void ScheduleNotificationOnExitFromARadius()
    {
        isClickedOnExit = true;
        if (!canUseCustomValues.isOn)
        {
            startingPointLatitude = Input.location.lastData.latitude;
            startingPointLongitude = Input.location.lastData.longitude;
        }
        var exitlocationTrigger = new iOSNotificationLocationTrigger()
        {
            Center = new Vector2(startingPointLatitude, startingPointLongitude),
            Radius = 5f, 
            NotifyOnEntry = false,
            NotifyOnExit = true,
        };
        Debug.Log("Push Notification is set for a radius of " + exitlocationTrigger.Radius.ToString() + "Meters"
          + " When user enters in " + "Latitude = " + Input.location.lastData.latitude + "===" + "Longitude = " + Input.location.lastData.longitude);

        var ExitBasedNotification = new iOSNotification()
        {
            Title = "Exited",
            Subtitle = "You Have Exited radius of " + exitlocationTrigger.Center + " Meters",
            Body = "Radius latitude was > " + startingPointLatitude + " and longitude was > " + startingPointLongitude,
            ShowInForeground = true,//This ShowInForeground variable will allow the notification to show in forground while the app is still opened
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            Trigger = exitlocationTrigger
        };
        //schedule notification for exit base
        iOSNotificationCenter.ScheduleNotification(ExitBasedNotification);
        Debug.Log("Started a location notification at " + exitlocationTrigger.Center);
    }
    //This function will cancel all the notifications for your app
    public void CancellAllNotification() {
        iOSNotificationCenter.RemoveAllScheduledNotifications();
    }
    bool flag = true;
    //This function calculates distance between two sets of coordinates, taking into account the curvature of the earth.
    public void CalculateTheDistanceBetweenCoordinatesAndCurrentCoordinates(float lat1, float lon1, float lat2, float lon2)
    {

        var R = 6378.137; // Radius of earth in KM
        var dLat = lat2 * Mathf.PI / 180 - lat1 * Mathf.PI / 180;
        var dLon = lon2 * Mathf.PI / 180 - lon1 * Mathf.PI / 180;
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
          Mathf.Cos(lat1 * Mathf.PI / 180) * Mathf.Cos(lat2 * Mathf.PI / 180) *
          Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        distance = R * c;
        distance = distance * 1000f; // meters

        //Showing the distance text on the canvas
        distanceDetails.text = "Distance: " + distance;

        if (distance > 30 && flag && isClickedOnExit) {
            flag = false;
            // Variable to determine the time period
            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                //Timeinterval should be atleast 1 min or 60 seconds
                TimeInterval = new TimeSpan(0, 1, 10),
                Repeats = false
            };
            var timeBasedNotification = new iOSNotification()
            {
                // You can specify a custom identifier which can be used to manage the notification later.
                // If you don't provide one, a unique string will be generated automatically.
                Identifier = Application.identifier,
                Title = "Localtion Notification",
                Body = "Scheduled at: " + DateTime.Now.Hour +" : " + DateTime.Now.Minute +" : " +DateTime.Now.Millisecond,
                Subtitle = DateTime.Now.Hour + " : " + DateTime.Now.Minute + " : " + DateTime.Now.Millisecond,
                ShowInForeground = true,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                CategoryIdentifier = "category_a",
                ThreadIdentifier = "thread1",
                Trigger = timeTrigger,
            };
            //schedule a time base notification
            iOSNotificationCenter.ScheduleNotification(timeBasedNotification);
        }
        //convert distance from double to float
        //float distanceFloat = (float)distance;
        
        //set the target position of the player, this is where we lerp to in the update function
        //targetPosition = originalPosition - new Vector3(0, 0, distanceFloat * 12);
        //distance was multiplied by 12 so I didn't have to walk that far to get the player to show up closer

    }
    //This function is just for a simple timebased local push notification system
    public void TimeBasedPushNotification()
    {
        // Variable to determine the time period
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            //Timeinterval should be atleast 1 min or 60 seconds
            TimeInterval = new TimeSpan(0, 1, 10),
            Repeats = false
        };
        var timeBasedNotification = new iOSNotification()
        {
            // You can specify a custom identifier which can be used to manage the notification later.
            // If you don't provide one, a unique string will be generated automatically.
            Identifier = Application.identifier,
            Title = "Trace",
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };
        //schedule a time base notification
        iOSNotificationCenter.ScheduleNotification(timeBasedNotification);
    }
    //Assign values to startingPointLatitude which we entered in inputfield
    public void OnLatitudeValueChange()
    {
        if (canUseCustomValues.isOn)
        {
            startingPointLatitude = float.Parse(latitudeInputField.text);
            Debug.Log(startingPointLatitude);
        }
    }
    //Assign values to startingPointLongitude which we entered in inputfield
    public void OnLongitudeValueChange()
    {
        if (canUseCustomValues.isOn)
        {
            startingPointLongitude = float.Parse(longitudeInputField.text);
            Debug.Log(startingPointLongitude);
        }
    }
    //Assign values to startingPointLatitude & startingPointLongitude from drop downline list
    public void SelectedDropDownValues()
    {
        if (canUseCustomValues.isOn)
        {
            if (locationList.value == 1)
            {
                startingPointLatitude = homeLat;
                startingPointLongitude = homeLong;
            }
            else if (locationList.value == 2)
            {
                startingPointLatitude = officeLat;
                startingPointLongitude = officeLong;
            }
            Debug.Log(locationList.value);
        }
    }
}
