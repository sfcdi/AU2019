### this will upload files to box

# load modules
import ftplib
from datetime import datetime
import glob, os

# Set your credentials information here
# your ftp site, like box
# you don't need the full path here, just the domain
my_ftp_site = 'ftp.box.com'

# enter your username and password
# obviously this script should be in a secure location since it contains your credentials
# If you are using box with single sign on (SSO) then you'll need to set your 
# box password in your account settings.  This password is seperate from your 
# SSO credentials.  You still use your SSO credentials to login.
my_username = 'user@domain.com'
my_password = 'password123456789'

# this is the Box folder we will be uploading to 
ftp_folder = 'project1/building/plot/'
Log(0, "Folders to Check", str(ftp_folders))

# the location of the dwg & nwc files on your server
my_folder = 'C:/project/folder'

# logging
# logs will be exported to the current folder
dir = os.getcwd() + "\\upload.log"
log = open(dir ,"a")
message = "Date: " + str(datetime.now()) + " Uploading \n"
log.write("\n" + ("#") * len(message) + "\n")
print("###################")
print(message)

log.write(message)
log.write((len(message)*"-") + "\n")

# catch the number of errors
errors = 0

# logs a message
def Log(level, action, message):
	# gets the log 
	global log
	
	# find the current date
	date = str(datetime.now())[11:19]
	
	# sets the level of the log
	if level == 0:
		level = "Info"
	if level == 1:
		level = "Warn" 
	
	# sets the message
	message = date + ":" + level + ":Program.Main:" + action + ":" + message + "\n"
	
	# logs it to the file
	log.write(message)
	
	# print a message to the console
	print(message)

# all the exported backgrounds will be stored in this list.
backgrounds = []

# this will connect to the Box ftp server
# I was unable to change folders after uploading files
# this will close the connection and reconnect for every folder
def Connect():
	Log(0, "Connecting", "Connecting to Box as " + my_username) 
	try:
		ftp = ftplib.FTP(my_ftp_site, my_username, my_password)
		ftp.set_debuglevel(0)
	except:
		Log(1, "Connecting", "Failed to connect to Box ftp server")
		errors += 1
		
	return ftp
		
# get all the dwg, nwc, and rvt to be uploaded based on the file name
for f in os.listdir(my_folder):		
	# get only dwg, nwc, or rvt files
	if f[-3:] == "dwg" or f[-3:] == "nwc" or f[-3:] == "rvt":
		
		# this will filter out duplicates
		if f not in backgrounds:
			backgrounds.append(f)
			
			# log the files that were found
			Log(0, "Finding Files", "Added file: " + str(f)) 
	
	

# connect to the ftp site
ftp = Connect()

# set the current folder
try:
	ftp.cwd(ftp_folder)
	Log(0, "Setting Current Box Folder", ftp_folder)
except:
	Log(1, "Setting Current Box Folder", "Unable to find folder")
	errors += 1

# iterate through all the files in this folder 
# and upload
for file in backgrounds:	
	
	try:
		with open(my_folder + file, 'rb') as d:
			ftp.storbinary('STOR %s' % file, d)
			Log(0, "Upload", "Uploaded " + file)
	except:
		Log(1, "Upload", "Unable to upload " + file)
		errors += 1				

# close the ftp connect
# a new connect will be created for the next folder
ftp.quit()
Log(0, "Connecting", "Connection closed")

Log(0, "Complete", "All files uploaded")
Log(0, "Complete", "Finished with " + str(errors) + " errors")
log.close()

print(backgrounds)