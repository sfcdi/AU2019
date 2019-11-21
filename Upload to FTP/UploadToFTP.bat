
:: fill in the following info
:: The first argument is the ftp site to and file upload to
:: you need the ftp:// prefix to tell curl this is the ftp server
:: then you include the fpt address, for for box it's ftp.box.com
:: Add the file name to the end
:: ftp.box.com/project/folder
:: then add your username and box password separated by a colon
:: if you use SSO you might need to edit your box password in account settings and use that
:: The BOX password for your account can be different from your SSO password
:: you still use your SSO password to login to Box manually
:: The filename is the filename of your project
:: the -T flag uses the filename as the name on BOX
:: example: curl -T "c:/projects/drawings/floorplan.pdf" ftp://ftp.box.com/project/drawings -user bob:notArealpass0rd
curl -T localfile ftp://SERVER_ADDRESS/FILENAME -user USERNAME:PASSWORD

:: This leaves the window open after uploading
cmd /k