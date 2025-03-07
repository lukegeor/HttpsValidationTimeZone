# HttpsValidationTimeZone
Demo of how changing the timezone while an https client is running can cause a validation error

## Problem Statement
While a .Net application is running, if you change the system time zone ahead (i.e. if your current time zone is US Pacific time and you change the system time zone to US Eastern time),
if your server has a newly-created TLS certificate, if the age of the certificate (not valid before date) is smaller than the amount of time you changed your time zone, certificate
validation fails with a NotTimeValid error in the certificate chain.

This only occurs on Linux. I tested on Oracle Linux 8. I'm using .Net SDK 8.0.303 and runtime 8.0.7.
This also only occurs if you've configured your HTTPS client to re-establish its connection with every request.
In other words, setting the following.

```csharp
httpClient.DefaultRequestHeaders.ConnectionClose = true;
```

## Steps To Reproduce
You can test this yourself with any HTTPS server where you're providing your own TLS cert, but I've provided a server in this repo.

### Create TLS Cert
First, create a TLS certificate on your system using OpenSSL.
Run these commands in the Server folder in this repo.
This will create a server certificate whose not valid before date is the current time.

```bash
openssl req -new -newkey rsa:2048 -days 365 -nodes -x509 -keyout server.key -out server.crt
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
```

In the first step, the important value to set is `localhost` for the Common Name.
In the second step, don't set an export password, just press enter twice when it asks for a password.
You now have a server certificate `server.crt` and a PKCS12 private key + server cert bundle `server.pfx`, which is convenient when running a .Net HTTPS server.
Make sure you keep the file names as-is here because the server code expects server.pfx.

### Trust TLS Cert
You'll need to trust this self-signed cert in order to avoid an untrusted root validation error.
On Oracle Linux 8, you do that by running the following.

```bash
sudo cp server.crt /etc/pki/ca-trust/source/anchors
sudo update-ca-trust enable
sudo update-ca-trust extract
```

On Ubuntu, you can run the following. I haven't tested to see if this issue exists on Ubuntu or any Debian-based distributions.

```bash
sudo cp server.crt /usr/local/share/ca-certificates
sudo update-ca-certificates
```

### Find Your Time Zone
You can check your system's list of time zones and get your current time zone with these commands.

```bash
timedatectl list-timezones
timedatectl status
```

I'm in US Pacific time so I'll be using that as an example. My time zone is `America/Los_Angeles`.
Choose a time zone east of your current location. I use `Asia/Singapore` for my test.

### Launch Your Server
Launch the server app by running the following from the root of the solution.

```bash
dotnet run --project Server --launch-profile https
```

### Launch Your Client
The client app will try to connect to the server app every 5 seconds and print the contents of the response, plus some debug time information.

```bash
dotnet run --project Client --launch-profile client
```

You'll see the client print some information every 5 seconds.

### Change Your Time Zone
Now the fun part where we demonstrate the bug. Change your system time zone by running the following.

```bash
sudo timedatectl set-timezone Asia/Singapore
```

After you execute this command, you'll see an exception in your client app, with `NotTimeValid` as the chain status.

I'm guessing you'd run into the same issue on the expiration date of the cert, if the time to expiration is smaller than the difference between your current time zone and a time zone west of you.

### Reset Your System
Don't forget to put your time zone back to what you had it set to before.
You should have seen what your current time zone is when you ran `timedatectl status`. Run

```bash
sudo timedatectl set-timezone <your old time zone>
```

to reset your system.
