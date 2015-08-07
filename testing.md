# Testing MissionLine

## FakeTwilio
The project includes FakeTwilio, a console application that simulates a phone call into the system. Using  this client can allow you to test the site without using minutes in your telephony service.


### Configuring FakeTwilio
There are two ways to configure the client:
1. Start FakeTwilio.exe with the address of the site: `FakeTwilio.exe https://MY.TESTSITE.COM/`
2. Put the site address in FakeTwilio.exe.config:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.6"/>
    </startup>
	<appSettings>
	  <add key="MissionLineUrl" value="https://MY.TESTSITE.COM" />
	</appSettings>
</configuration>
```

### Running FakeTwilio
When you start the test client, it will prompt you for the phone number you are "calling" from. This should be entered as a string of ten digits (`2065551234`) followed by `Enter`

The client will then display an XML response representing what the phone menu would say back to the caller:
```xml
<Response>
  <Gather numDigits="1" action="https://MY.TESTSITE.COM/api/voice/DoMenu?" timeout="10">
    <Say voice="woman">Press 1 to sign in as Matt Cosand</Say>
    <Say voice="woman">Press 3 to record a message</Say>
    <Say voice="woman">Press 8 to change current responder</Say>
    <Say voice="woman">Press 9 for admin options</Say>
  </Gather>
</Response>
Enter digits:
```

If the message contains a `<Gather>` block, you will be prompted for the digits that the caller would enter on their keypad. Simply type the digits and hit `Enter`. If you are prompted to press `#`, simply press `Enter` instead.

If there are multiple `<Gather>` blocks, the test client will first ask you which block (task) you are responding to.

When you are finished with your call, enter `h` then `Enter` to hang up.