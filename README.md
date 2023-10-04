# ZapClient
This library is used for the connection of the most ZapTools and the Broker. 
You open a connection, retrive data and close it at the end. ZapClient has 
special functions for loading data for patients, a plan or plan details.

## Building
You have to add the two DLLs "zsClient.dll" and "zsUtilities.dll" from your 
system to the root folder. You should find both on your TPS system.  

## Installation
If you don't added the DLLs when you build the library by your own, you have 
to add the two DLLs "zsClient.dll" and "zsUtilities.dll" from your system to 
the folder, where you store the ZapClient. You should find both on your TPS 
system.  

Next step is to create a config file. ZapClient looks for it in the active 
folder (folder, where ZapClient is installed too). Normally it should have 
the name "ZapClient.cfg". If it doesn't exists, then ZapClient creates a new 
one with default values. It assumes, that ZapClient is on a machine in the 
Zap network (10.0.0.x). If this is the case, the Broker should be reachable 
via IP adress 10.0.0.105 and port 8088.

It could be, that you want to access the Broker on a machine, that isn't in 
the Zap network. If this is the case, you could provide an own config file. 
This config file should have the name "ZapClient.<Hostname>.cfg" or 
"ZapClient.<IP adress>.cfg". If the ZapClient finds such a file, it uses 
this for connecting to the Broker.

If you have a special user with access to the data and you don't want to 
enter credential data each time, you could provide username and password 
for this user in the config file. If the flag "Encryption" is set to "true",
the given password is encrypted. Sure, it isn't very good encrypted, but it 
is not visible on the first view. To create the encrypted password, follow 
this guid.

Creation of an encrypted passwords
1. Open a PowerShell
2. Enter the following line and replace "Your password" with your real password
     
      "Your password" | ConvertTo-SecureString -AsPlainText -Force | ConvertFrom-SecureString

3. Copy the given string into the config file
4. Save the file

## Constructor
To create a ZapClient object you need a function, that returns a string tupel 
with two strings for username and password. You are responsible for providing 
this information or asking the user for it. Additionally you could provide a 
logger to save information about what is happening. ZapClient use NLog for this.

## Connection
To work with connections, ZapClient has 2 functions.

### OpenConnection
This establish a connection with the Broker. It tries to use the last known 
username and password. If this doesn't work, it calls the function for getting 
username and password, that was provided when creating the object via constructor. 
It ask for this information as long as a connection could be established. You are 
responsible to stop this by returning two empty strings. If this is the case, 
the OpenConnection return false, else true.

### CloseConnection
The object closes the connection to the broker. This should be called always at 
the end of retriving data.

### Username (string)
Username property which contains the string with the username used for the active 
connection.

### Password (string)
Password property which contains the string with the password used for the active 
connection.

### IsConnected (bool)
Returns true, if the ZapClient is connected to the Broker

## Retriving data from Broker

### GetUsers
Get a list with all registered users in the TPS. Users could be created via the 
web interface. Each user belongs to one or more groups.

### GetPatientsWithStatus
Get a list with patients to the given patient status. Default is 
PatientStatus.Planning.

### GetAllPatients
Get a list with all patients. This are patients with PatientStatus of New, 
Planning, Treating, Treaded, Archived.

### GetPlansForPatient 
Get a list of all plans for a given Patient.

### GetPictureForPatient
Get the picture for a given Patient. It returns a byte array, which contains 
the image data. Could be of any type (JPG, PNG and so on).

### GetDicomSeriesForUuid
Get all Orthanc DICOM series for the given Patient and then returns the DICOM 
series for the given Orthanc UUID as DicomSeries.

### GetPlanDataForPlan
Get PlanData for a given Plan.

### GetPlanSummaryForPlan
Get PlanSummary for a given Plan.

### GetVOIsForPlan
Get VOIs for a given Plan.

### GetDVDataForPlan
Get DV data for a given Plan.

### GetBeamsForPlan
Get BeamData for a given Plan.

### GetSystemDataForPlan
Get the used SystemData for a given Plan.

### GetDoseVolumeGridForPlan
Get the dose volume grid for a given Plan. It returns a DoseVolumeGrid. Dose 
volume files are saved as byte arrays, containing beside the dose information 
also information about size and minimum and maximum values for dose.

### GetBeamsForTreatment
Get the DeliveryBeamSet for a given treated Plan.

### GetTreatmentDataForFraction
Get TreatmentReportData for a given treated Fraction.

### GetRadiationDataForFraction
Get RadiationTimeData for a given treated fraction. While loading, it 
calculates the field KVDoseMicroGy for each image, because it isn't filled by 
Zap.
