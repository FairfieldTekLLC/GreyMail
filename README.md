# GreyMail

# Leveraging AI to process emails to remove promotional and spam from your mailbox in Office 365.

# Update

- Added support for dragging a email out of the Promotions folder will now whitelist the domain
- Added a WhiteList parameter  to the config file


# Description

This program uses the Microsoft Graph Library and Ollama to determine if an email is a promotional email.
Currently, it is configured to use Ollama's LLM models to process the determination, but it could be easily switched out to any of the other AI tool chains.
To set this up you need to have:
	- A Microsoft 365 Business Email Account
	- A Computer capable of running Ollama, (it does work with there cloud solution)

The first step is to log into [https://entra.microsoft.com/](https://entra.microsoft.com/)
Then go to App Registrations
Then create a new application
Create a client secret for the new application.
Add the permission Mail.readwrite

Then edit the config file with the parameters.