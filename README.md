Panopto-BatchUserCreationWithGroupGUI
=====================

Panopto Batch User Creation With Group GUI

The program is directed towards a test server by default. User can change this in App.config or BatchUserCreationWithGroupGUI.exe.config. Simply change the server address in address property in all endpoint parameter to the user's desired server address.

Program will create users listed in the given CSV file using the given admin account and outputing any error messages on the error log.

	User Name: admin account user name
	Password: admin account password
	File: CSV file containing new user information
	Error Log: display error messages

Due to the structure of the code, folder is created first, then users, then it links the user with the groups user is assigned to and create them if such group does not exist, then linking user to folder. If any part fails, previous action will still have been completed.

Groups are not a required part of the CSV file, and there can by any number of groups listed for a single user.

CSV File Format: UserName,FirstName,LastName,EmailAddress[, Groups]

Sample CSV file format:

	aaa,aa,a,aaa@foo.com,foo,bar,otherGroups
	aab,aa,b,aab@foo.com,foo,bar
	aac,aa,c,aac@foo.com
