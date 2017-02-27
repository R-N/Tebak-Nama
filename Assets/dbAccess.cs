using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Data;
using System.Text;
using System.Security;
using System.Security.AccessControl;
using Mono.Data.SqliteClient;

public class dbAccess : MonoBehaviour {
	private string connection;
	public IDbConnection dbcon;
	public IDbCommand dbcmd;
	public IDataReader reader;
	public StringBuilder builder;

	public string packageName = "com.tmj16.nama3";
	string dir = null;
	string dbName = null;

	string filepath = null;

	public bool loaded = false;

	public bool alwaysDeleteDB = false;

	public bool newDB = false;

	// Use this for initialization
	void Start () {
		
	}
	
	public void OpenDB(string p)
	{
		Debug.Log("Call to OpenDB:" + p);
		// check if file exists in Application.persistentDataPath
		if (Application.platform == RuntimePlatform.Android)
			dir = "/data/data/" + packageName + "/files";
		else
			dir = Application.persistentDataPath;
		 filepath = dir +  Path.AltDirectorySeparatorChar + p + ".db";
		 dbName = p;

		if (File.Exists (filepath) && (alwaysDeleteDB || PlayerPrefs.GetString ("version") != Application.version)) {
			DeleteDB ();
			PlayerPrefs.SetString ("version", Application.version);
		}

		if(!File.Exists(filepath))
		{
			Debug.Log("Deploying db");
			string path;
			if (Application.platform == RuntimePlatform.Android)
				path = "jar:file://" + Application.dataPath + "!/assets/" + p + ".db";
			else
				path = "file://" + Application.streamingAssetsPath + Path.AltDirectorySeparatorChar + p + ".db";
			
			// if it doesn't ->
			// open StreamingAssets directory and load the db -> 
			WWW loadDB = new WWW(path);
			while(!loadDB.isDone) {
			}
			// then save to Application.persistentDataPath
			if (File.Exists (filepath)) {
				Debug.Log ("File exists!");
			} else {
				File.WriteAllBytes (filepath, loadDB.bytes);
				if (File.Exists (filepath)) {
					FileStream file = File.Open (filepath, FileMode.Open);
					if (file.Length == loadDB.bytes.LongLength) {
						Debug.Log ("Write succeeded. bytes : " + file.Length);
						newDB = true;
					}else
						Debug.Log ("Write failed - size mismatch " + file.Length + " should be " + loadDB.bytes.LongLength);
					

					file.Close ();
				} else {
					Debug.Log ("Write failed - file doesn't exist");
				}
			}
		}
		
		//open db connection
		connection = "URI=file:" + filepath;
		Debug.Log("Stablishing connection to: " + connection);
		dbcon = new SqliteConnection(connection);
		dbcon.Open();


		dbcmd = dbcon.CreateCommand();

		// WAL = write ahead logging, very huge speed increase
		dbcmd.CommandText = "PRAGMA journal_mode = WAL;";
		dbcmd.ExecuteNonQuery();

		// journal mode = look it up on google, I don't remember
		dbcmd.CommandText = "PRAGMA journal_mode";
		reader = dbcmd.ExecuteReader();
		if (reader.Read())
			Debug.Log("SQLiter - WAL value is: " + reader.GetString(0));
		reader.Close();

		// more speed increases
		dbcmd.CommandText = "PRAGMA synchronous = OFF";
		dbcmd.ExecuteNonQuery();

		// and some more
		dbcmd.CommandText = "PRAGMA synchronous";
		reader = dbcmd.ExecuteReader();
		if (reader.Read())
			Debug.Log("SQLiter - synchronous value is: " + reader.GetInt32(0));
		loaded = true;
	}
	
	public void CloseDB(){
		if (reader != null) {
			reader.Close (); // clean everything up
			reader = null;
		}
		if (dbcmd != null) {
			dbcmd.Dispose ();
			dbcmd = null;
		}
		if (dbcon != null) {
			dbcon.Close ();
			dbcon = null;
		}
		if(alwaysDeleteDB)
			DeleteDB ();
	}

	public void DeleteDB(){
		Debug.Log ("Deleting DB");
		GC.Collect ();
		GC.WaitForPendingFinalizers ();
		if (!string.IsNullOrEmpty (dir) && !string.IsNullOrEmpty (dbName)) {
			if (File.Exists (filepath))
				File.Delete (filepath);
			if (File.Exists (filepath + "-shm"))
				File.Delete (filepath + "-shm");
			if (File.Exists (filepath + "-wal"))
				File.Delete (filepath + "-wal");
		} else {
			Debug.Log ("dir " + dir);
			Debug.Log ("dbName " + dbName);
		}
	}
	
	public IDataReader BasicQuery(string query){ // run a basic Sqlite query
		dbcmd = dbcon.CreateCommand(); // create empty command
		dbcmd.CommandText = query; // fill the command
		reader = dbcmd.ExecuteReader(); // execute command which returns a reader
		return reader; // return the reader
	
	}
	
	
	public bool CreateTable(string name,string[] col, string[] colType){ // Create a table, name, column array, column type array
		string query;
		query  = "CREATE TABLE " + name + "(" + col[0] + " " + colType[0];
		for(var i=1; i< col.Length; i++){
			query += ", " + col[i] + " " + colType[i];
		}
		query += ")";
		try{
			dbcmd = dbcon.CreateCommand(); // create empty command
			dbcmd.CommandText = query; // fill the command
			reader = dbcmd.ExecuteReader(); // execute command which returns a reader
		}
		catch(Exception e){
			
			Debug.Log(e);
			return false;
		}
		return true;
	}
	
	public int InsertIntoSingle(string tableName, string colName , string value ){ // single insert
		string query;
		query = "INSERT INTO " + tableName + "(" + colName + ") " + "VALUES (" + value + ")";
		try
		{
			dbcmd = dbcon.CreateCommand(); // create empty command
			dbcmd.CommandText = query; // fill the command
			reader = dbcmd.ExecuteReader(); // execute command which returns a reader
		}
		catch(Exception e){
			
			Debug.Log(e);
			return 0;
		}
		return 1;
	}
	
	public int InsertIntoSpecific(string tableName, string[] col, string[] values){ // Specific insert with col and values
		string query;
		query = "INSERT INTO " + tableName + "(" + col[0];
		for(int i=1; i< col.Length; i++){
			query += ", " + col[i];
		}
		query += ") VALUES (" + values[0];
		for(int i=1; i< col.Length; i++){
			query += ", " + values[i];
		}
		query += ")";
		Debug.Log(query);
		try
		{
			dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = query;
			reader = dbcmd.ExecuteReader();
		}
		catch(Exception e){
			
			Debug.Log(e);
			return 0;
		}
		return 1;
	}
	
	public int InsertInto(string tableName , string[] values ){ // basic Insert with just values
		string query;
		query = "INSERT INTO " + tableName + " VALUES (" + values[0];
		for(int i=1; i< values.Length; i++){
			query += ", " + values[i];
		}
		query += ")";
		try
		{
			dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = query;
			reader = dbcmd.ExecuteReader();
		}
		catch(Exception e){
			
			Debug.Log(e);
			return 0;
		}
		return 1;
	}
	
	public ArrayList SingleSelectWhere(string tableName , string itemToSelect,string wCol,string wPar, string wValue){ // Selects a single Item
		string query;
		query = "SELECT " + itemToSelect + " FROM " + tableName + " WHERE " + wCol + wPar + wValue;	
		dbcmd = dbcon.CreateCommand();
		dbcmd.CommandText = query;
		reader = dbcmd.ExecuteReader();
		//string[,] readArray = new string[reader, reader.FieldCount];
		string[] row = new string[reader.FieldCount];
		ArrayList readArray = new ArrayList();
		while(reader.Read()){
			int j=0;
			while(j < reader.FieldCount)
			{
				row[j] = reader.GetString(j);
				j++;
			}
			readArray.Add(row);
		}
		return readArray; // return matches
	}

	// Update is called once per frame
	void Update () {
		
	}
		
}