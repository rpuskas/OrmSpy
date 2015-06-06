Goals of project:

Diagnostics:

	Provide lightweight mechanism for getting feedback on SQL operations for an ORM Unit of Word (console friendly)
	- Shows number of SQL calls
	- Translates SQL calls into parsable native SQL suitable for SQL server execution
		- Parses into human readable (tabbed) format?
	- Shows Rows returned from Operation (and size of response?)
	- Works for very large transaction volumes with minnimal affect to execution time

Testing:

	Allows for assertions to be thrown if exeeding a SQL threshold in:
	- Execution time
	- Number of calls
	- Rows returned

