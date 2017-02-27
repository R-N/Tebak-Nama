using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Mono.Data.SqliteClient;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;


public class MainScript : MonoBehaviour {

	public struct ProtectedInt{
		private int saltedValue;
		private int salt;

		public int value{
			get{
				int val = saltedValue - salt;
				value = val;
				return val;
			}
			set {
				salt = UnityEngine.Random.Range (-2500, 2500);
				saltedValue = value + salt;
			}
		}
	}
	public struct ProtectedLong{
		private long saltedValue;
		private int salt;

		public long value{
			get{
				long val = saltedValue - salt;
				value = val;
				return val;
			}
			set {
				salt = UnityEngine.Random.Range (-2500, 2500);
				saltedValue = value + salt;
			}
		}
	}
	
	public static MainScript Instance = null;
	public bool DebugMode = false;
	
	/// <summary>
	/// Table name and DB actual file location
	/// </summary>
	private const string SQL_DB_NAME = "database";
	
	// feel free to change where the DBs are stored
	// this file will show up in the Unity inspector after a few seconds of running it the first time
	private static  string SQL_DB_LOCATION = null;
	
	// table name
	private const string SQL_TABLE_NAME = "TabelAngkatan";

	
	/// <summary>
	/// DB objects
	/// </summary>
	private IDataReader mReader = null;
	private string mSQLString;
	
	public bool mCreateNewTable = false;

	public GameObject mainPanel = null;

	public RawImage imgField = null;
	public MyScrollRect imgFieldScroll = null;
	public InputField nameField = null;
	public InputField asalField = null;
	 string[] namaArray = null;
	 string[] panggilanArray = null;
	 string[] asalArray = null;
	 long nrpCur = 0;

	public GameObject playPanel = null;
	public GameObject infoPanel = null;
	public RawImage infoImage = null;
	public MyScrollRect infoImageScroll = null;
	public Text infoText = null;
	public ScrollRect infoTextScroll = null;

	public Texture noImage = null;

	public GameObject benar = null;
	public GameObject salah = null;
	public GameObject hampir = null;

	public InputField searchField = null;
	public Transform searchResults = null;
	public ScrollRect searchResultsRect = null;

	public GameObject panelCari = null;

	public GameObject backToCari = null;
	public GameObject backToPlay = null;
	public GameObject backToPlayPilgan = null;

	public dbAccess db = null;

	public Dictionary<long, Texture> imageCache = new Dictionary<long, Texture>();

	List<long> nrpHistory = new List<long>();
	List<long> nrpWithoutPhoto = new List<long>();

	ProtectedInt streak = new ProtectedInt();
	ProtectedLong score = new ProtectedLong();

	public Text scoreText = null;

	ProtectedInt passes = new ProtectedInt();

	string nrpHistoryString = "";
	string noPhotoString = "";
	string exclusionString = "";

	public bool allowNoAsal = false;

	public Vector2 scrollCenter = new Vector2(0.5f, 0.5f);

	string prevSearch = null;

	int lowestScore = 0;

	bool ready = false;

	public GameObject playPilganPanel = null;
	public Dropdown namaDropdown = null;
	public Dropdown asalDropdown = null;
	public RawImage imgFieldPilgan = null;
	public MyScrollRect imgFieldPilganScroll = null;

	int namaPos = 0;
	int prevNamaPos = 0;
	int asalPos = 0;
	int prevAsalPos = 0;
	/// <summary>
	/// Awake will initialize the connection.  
	/// RunAsyncInit is just for show.  You can do the normal SQLiteInit to ensure that it is
	/// initialized during the AWake() phase and everything is ready during the Start() phase
	/// </summary>
	/// 

	public Dropdown sortDropdown = null;
	string[] sorting = new string[]{"Nama", "NRP"};
	public GameObject NRPRangePanel;
	public InputField NRPAwalField;
	public InputField NRPAkhirField;
	public GameObject errorPanel;
	public Text errorText;
	private string NRPAwal;
	private string NRPAkhir;
	private string nrpRange;

	void Awake()
	{
		streak.value = 0;
		score.value = 0;
		passes.value = 3;
		db.OpenDB ("database");
	}

	
	void Start()
	{
		StartCoroutine ("WaitForDB");
		imgField.texture = null;
		Instance = this;
		LoadScore ();
		sortDropdown.onValueChanged.AddListener (ChangeSorting);
		this.NRPAwal = PlayerPrefs.GetString("nrpawal");
		this.NRPAkhir = PlayerPrefs.GetString("nrpakhir");
		SetNRPRangeField ();
		SetNRPRange ();
	}

	void ChangeSorting(int sortMode){
		this.prevSearch = null;
		this.Cari();
	}

	public void SetNRPRangeField()
	{
		this.NRPAwalField.text = this.NRPAwal;
		this.NRPAkhirField.text = this.NRPAkhir;
	}
	public void SetNRPRange()
	{
		this.NRPAwal = this.NRPAwalField.text;
		this.NRPAkhir = this.NRPAkhirField.text;
		long naw = 0;
		long nak = 0;
		if (!string.IsNullOrEmpty(NRPAwal) && !long.TryParse(this.NRPAwal, out naw))
			this.ShowError("NRP Awal harus angka.");
		else if (!string.IsNullOrEmpty(NRPAkhir) && !long.TryParse(this.NRPAkhir, out nak))
		{
			this.ShowError("NRP Akhir harus angka.");
		}
		else
		{
			string str = string.Empty;
			if (string.IsNullOrEmpty (NRPAwal)) {
				if (!string.IsNullOrEmpty (NRPAkhir)) {
					str = " AND NRP <= " + nak;
				}
			} else{
				if (string.IsNullOrEmpty (NRPAkhir)) {
					str = " AND NRP >= " + naw;
				} else {
					if (naw > nak) {
						str = " AND (NRP <= " + nak + " OR NRP >= " + naw + ")";
					} else {
						str = " AND NRP >= " + naw + " AND NRP <= " + nak;
					}
				}
			}


			this.mReader = this.db.BasicQuery("SELECT * FROM TabelAngkatan WHERE 1=1 " + str);
			for (int index = 0; index < 5; ++index)
			{
				if (!this.mReader.Read())
				{
					this.ShowError("Dalam selang NRP harus ada minimal 5 entry");
					this.mReader.Close();
					return;
				}
			}
			this.nrpRange = str;
			PlayerPrefs.SetString("nrpawal", (naw == 0 ? string.Empty : naw.ToString()));
			PlayerPrefs.SetString("nrpakhir", (nak == 0 ? string.Empty : nak.ToString()));
			this.NRPRangePanel.SetActive(false);
			this.mainPanel.SetActive(true);
		}
	}

	public int ShowError(string err)
	{
		this.errorText.text = err;
		this.errorPanel.SetActive(true);
		return 0;
	}

	IEnumerator WaitForDB(){
		while (!db.loaded) {
			yield return new WaitForEndOfFrame ();
		}
		if (db.newDB) {
			db.BasicQuery ("UPDATE TabelAngkatan SET Score = 0 WHERE Score IS NULL");
			mReader = db.BasicQuery ("SELECT NRP FROM TabelAngkatan");
			while (mReader.Read ()) {
				string nrp = mReader.GetInt64 (0).ToString();
				if (PlayerPrefs.HasKey (nrp)) {
					db.BasicQuery ("UPDATE TabelAngkatan SET Score = " + PlayerPrefs.GetInt (nrp) + " WHERE NRP = " + nrp);
				} else {
					PlayerPrefs.SetInt (nrp, 0);
				}
			}
			mReader.Close ();
		}
		ready = true;
	}

	public void GetLowestScore(){
		string query = "SELECT Score FROM TabelAngkatan WHERE Score IS NOT NULL";
		if (!allowNoAsal)
			query = query + " AND ASAL IS NOT NULL AND ASAL <> '' AND ASAL <> ' '";

		mReader = db.BasicQuery (query + exclusionString + " ORDER BY Score ASC LIMIT 1");
		mReader.Read ();
		lowestScore = mReader.GetInt32 (0);
	}

	public void GetRandom(){
		//StopCoroutine ("GetRandomCor");
		//StartCoroutine ("GetRandomCor");
		GetLowestScore();
		GetRandomActual();
	}

	public IEnumerator GetRandomCor(){
		while (!ready) {
			yield return new WaitForEndOfFrame ();
		}
		GetRandomActual ();
	}

	public void RefreshnrpHistoryString(){
		int historyLength = nrpHistory.Count;
		nrpHistoryString = "";
		for (int a = 0; a < historyLength; a++) {
			nrpHistoryString += " AND NRP <> " + nrpHistory [a];
		}
		exclusionString = noPhotoString + nrpHistoryString;
	}

	public void RefreshnoPhotoString(){
		int noPhotoLength = nrpWithoutPhoto.Count;
		noPhotoString = "";
		for (int a = 0; a < noPhotoLength; a++) {
			noPhotoString += " AND NRP <> " + nrpWithoutPhoto [a];
		}
		exclusionString = noPhotoString + nrpHistoryString;
	}

	public void GetRandomActual(){
		string query = "SELECT NRP, Nama, Panggilan, Daerah_Asal FROM TabelAngkatan WHERE Score <= " + (lowestScore + 20);
		if (!allowNoAsal)
			query = query + " AND Daerah_Asal IS NOT NULL AND Daerah_Asal <> '' AND Daerah_Asal <> ' '";
		//Debug.Log (query + " ORDER BY RANDOM() LIMIT 1 OFFSET " + offset);
		mReader = db.BasicQuery (query + exclusionString + " ORDER BY RANDOM() LIMIT 1");
		mReader.Read ();
		nrpCur = mReader.GetInt64 (0);
		
		imgField.texture = LoadFoto(nrpCur);
		if (imgField.texture == noImage || imgField.texture == null) {
			nrpWithoutPhoto.Add (nrpCur);
			RefreshnoPhotoString ();
			mReader.Close ();
			GetRandom ();
			return; 
		}
		SetScale (imgField);

		nrpHistory.Add (nrpCur);
		while (nrpHistory.Count > 30) {
			nrpHistory.RemoveAt (0);
		}
		RefreshnrpHistoryString ();

		if (mReader.IsDBNull (1)) {
			namaArray = null;
		} else {
			string str = mReader.GetString (1);
			if (string.IsNullOrEmpty (str))
				namaArray = null;
			else
				namaArray = mReader.GetString (1).ToLower ().Split (' ');
		}
		if (mReader.IsDBNull (2)) {
			panggilanArray = null;
		} else {
			string str = mReader.GetString (2);
			if (string.IsNullOrEmpty (str))
				panggilanArray = null;
			else
			panggilanArray = mReader.GetString (2).ToLower ().Split (',', ' ');
		}
		if (mReader.IsDBNull (3)) {
			if (allowNoAsal) {
				asalArray = null;
				asalField.gameObject.SetActive (false);
			}else {
				nrpWithoutPhoto.Add (nrpCur);
				RefreshnoPhotoString ();
				mReader.Close ();
				GetRandom ();
			}
		} else {
			string str = mReader.GetString (3);
			if (string.IsNullOrEmpty (str)) {
				if (allowNoAsal) {
					asalArray = null;
					asalField.gameObject.SetActive (false);
				} else {
					nrpWithoutPhoto.Add (nrpCur);
					RefreshnoPhotoString ();
					mReader.Close ();
					GetRandom ();
				}
			} else {
				asalField.gameObject.SetActive (true);
				asalArray = mReader.GetString (3).ToLower ().Replace (" ", "").Split (',');
			}
		}
		mReader.Close ();
		//imgField.texture = noImage;
		//StartCoroutine (LoadFotoCor (nrp, imgField));
		imgFieldScroll.normalizedPosition = scrollCenter;
		playPanel.SetActive (true);
		mainPanel.SetActive (false);
		infoPanel.SetActive (false);
	}


	public void GetRandomPilgan(){
		string query = "SELECT NRP, Nama, Panggilan, Daerah_Asal, Jenis_Kelamin FROM TabelAngkatan WHERE Score <= " + (lowestScore + 20);
		if (!allowNoAsal)
			query = query + " AND Daerah_Asal IS NOT NULL AND Daerah_Asal <> '' AND Daerah_Asal <> ' '";
		mReader = db.BasicQuery (query + exclusionString + " ORDER BY RANDOM() LIMIT 1");
		mReader.Read ();
		nrpCur = mReader.GetInt64 (0);

		imgFieldPilgan.texture = LoadFoto(nrpCur);
		if (imgFieldPilgan.texture == noImage || imgFieldPilgan.texture == null) {
			nrpWithoutPhoto.Add (nrpCur);
			RefreshnoPhotoString ();
			mReader.Close ();
			GetRandomPilgan ();
			return; 
		}
		SetScale (imgFieldPilgan);

		nrpHistory.Add (nrpCur);
		while (nrpHistory.Count > 30) {
			nrpHistory.RemoveAt (0);
		}
		RefreshnrpHistoryString ();


		if (namaPos == 0 || UnityEngine.Random.Range(0, 1) == 1) {
			namaPos = UnityEngine.Random.Range (1, 5);
		} else {
			int[] namaPosPool = new int[4];
			int a = 0;
			for (int i = 1; i <= 5; i++) {
				if (i != prevNamaPos) {
					namaPosPool [a] = i;
					a++;
				}
			}
			namaPos = namaPosPool[UnityEngine.Random.Range (0, 3)];
		}

		prevNamaPos = namaPos;

		string namaString = mReader.GetString (1);
		string asalString = null;

		if (!mReader.IsDBNull (2)){
			string panggilanString = mReader.GetString (2);
			if (!string.IsNullOrEmpty (panggilanString))
				namaString += " (" + mReader.GetString (2) + ")";
		}

		string jk = null;
		if (!mReader.IsDBNull(4))
			jk = mReader.GetString (4);

		query = "SELECT Nama, Panggilan FROM TabelAngkatan WHERE  Score <= " + (lowestScore + 40) + (!string.IsNullOrEmpty(jk) ? (" AND Jenis_Kelamin = '" + jk + "'") : "");
		IDataReader readNama = db.BasicQuery (query  + " AND NRP <> " + nrpCur + " ORDER BY RANDOM() LIMIT 4");

		for (int i = 1; i <= 5; i++) {
			if (i == namaPos) {
				namaDropdown.options [i].text = namaString;
			} else {
				readNama.Read ();
				string namaLain = readNama.GetString (0);
				if (!readNama.IsDBNull (1)) {
					string p = readNama.GetString (1);
					if (!string.IsNullOrEmpty (p))
						namaLain += " (" + p + ")";
				}
				namaDropdown.options [i].text = namaLain;
			}
		}
		namaDropdown.value = 0;
		namaDropdown.RefreshShownValue ();
		
		if (mReader.IsDBNull (3)) {
			if (allowNoAsal) {
				asalDropdown.gameObject.SetActive (false);
				asalPos = 0;
			}else {
				nrpWithoutPhoto.Add (nrpCur);
				RefreshnoPhotoString ();
				mReader.Close ();
				GetRandomPilgan ();
			}
		} else {
			string str = mReader.GetString (3);
			if (string.IsNullOrEmpty (str)) {
				if (allowNoAsal) {
					asalDropdown.gameObject.SetActive (false);
					asalPos = 0;
				}else {
					nrpWithoutPhoto.Add (nrpCur);
					RefreshnoPhotoString ();
					mReader.Close ();
					GetRandomPilgan ();
				}
			} else {
				asalDropdown.gameObject.SetActive (true);
				asalString = mReader.GetString (3);

				query = "SELECT DISTINCT Daerah_Asal FROM TabelAngkatan WHERE Daerah_Asal IS NOT NULL AND Daerah_Asal <> '' AND Daerah_Asal <> ' ' ";
				IDataReader readAsal = db.BasicQuery (query + " AND NRP <> " + nrpCur + " ORDER BY RANDOM() LIMIT 4");

				if (prevAsalPos == 0 || UnityEngine.Random.Range(0, 1) == 1) {
					asalPos = UnityEngine.Random.Range (1, 5);
				} else {
					int[] asalPosPool = new int[4];
					int b = 0;
					for (int i = 1; i <= 5; i++) {
						if (i != prevAsalPos) {
							asalPosPool [b] = i;
							b++;
						}
					}
					asalPos = asalPosPool[UnityEngine.Random.Range (0, 3)];
				}
				prevAsalPos = asalPos;
				for (int i = 1; i <= 5; i++) {
					if (i == asalPos) {
						asalDropdown.options [i].text = asalString;
					} else {
						readAsal.Read ();
						asalDropdown.options [i].text = readAsal.GetString (0);
					}
				}
				asalDropdown.value = 0;
				asalDropdown.RefreshShownValue ();
			}
		}
		mReader.Close ();


		//imgField.texture = noImage;
		//StartCoroutine (LoadFotoCor (nrp, imgField));
		imgFieldPilganScroll.normalizedPosition = scrollCenter;
		playPilganPanel.SetActive (true);
		mainPanel.SetActive (false);
		infoPanel.SetActive (false);
	}

	public long CalculateScore(int correctness){
		return correctness + (long)Mathf.Pow ((float)streak.value, 2.0f);
	}

	public void CekJawabanPilgan(){
		bool nama = namaDropdown.value == namaPos;
		bool asalNull = asalPos == 0 || !asalDropdown.gameObject.activeInHierarchy;
		Debug.Log ("asalvalue " + asalDropdown.value + " asalPos " + asalPos + " asalnull " + asalNull);
		bool asal = !asalNull && asalDropdown.value == asalPos;
		int correctness = 0;
		if (nama && asal) {
			benar.SetActive (true);
			correctness += 7;
			streak.value = streak.value + 1;
		} else if (nama && asalNull) {
			benar.SetActive (true);
			correctness += 5;
		} else if (nama) {
			hampir.SetActive (true);
			correctness += 2;
		} else if (asal) {
			hampir.SetActive (true);
			correctness += 1;
		}else{
			salah.SetActive(true);
			streak.value = 0;
			passes.value = 3;
		}
		long newScore = CalculateScore (correctness);
		score.value = score.value + newScore;
		string nrpString = nrpCur.ToString ();
		PlayerPrefs.SetInt (nrpString, PlayerPrefs.GetInt (nrpString) + correctness);
		db.BasicQuery ("UPDATE TabelAngkatan SET Score = " + PlayerPrefs.GetInt (nrpString) + " WHERE NRP = " + nrpString);
		SaveScore ();
		RefreshScore ();
		playPilganPanel.SetActive (false);
		backToPlayPilgan.SetActive (true);
		TampilkanInfo (nrpCur, imgFieldPilganScroll);
	}

	public void CekJawaban(){
		playPanel.SetActive (false);
		string jawabNama = nameField.text.ToLower ();
		bool nama = !string.IsNullOrEmpty (jawabNama);
		int cNama = 1;
		if (nama) {
			string[] parsedNama0 = jawabNama.Split (' ');
			List<string> parsedNama = new List<string> ();
			int count = parsedNama0.Length;
			for (int i = 0; i < count; i++) {
				if (!string.IsNullOrEmpty (parsedNama0 [i]) && !parsedNama.Contains (parsedNama0 [i]))
					parsedNama.Add (parsedNama0 [i]);
			}
			count = parsedNama.Count;
			nama =  count > 0;
			if (nama) {
				for (int i = 0; i < count; i++) {
					bool dNama = namaArray.Contains (jawabNama) || (panggilanArray != null && panggilanArray.Contains (jawabNama));
					nama = nama && dNama;
					if (dNama)
						cNama++;
					else
						cNama--;
				}
			}
		}

		string jawabAsal = asalField.text.ToLower().Replace(" ", "");
		bool asalNull = asalArray == null;
		bool asal = !asalNull && !string.IsNullOrEmpty (jawabAsal) && asalArray.Contains (jawabAsal);
		string nrpString = nrpCur.ToString ();
		int correctness = 0;
		if (nama && asal) {
			benar.SetActive (true);
			correctness += 9;
			streak.value = streak.value + 1;
		} else if (nama && asalNull) {
			benar.SetActive (true);
			correctness += 7;
		} else if (nama) {
			hampir.SetActive (true);
			correctness += 4;
		} else if (asal) {
			hampir.SetActive (true);
			correctness += 3;
		} else if (cNama > 0){
			hampir.SetActive (true);
		}else{
			salah.SetActive(true);
			streak.value = 0;
			passes.value = 3;
		}
		correctness += cNama;
		long newScore = CalculateScore (correctness);
		score.value = score.value + newScore;
		PlayerPrefs.SetInt (nrpString, PlayerPrefs.GetInt (nrpString) + correctness);
		db.BasicQuery ("UPDATE TabelAngkatan SET Score = " + PlayerPrefs.GetInt (nrpString) + " WHERE NRP = " + nrpString);
		SaveScore ();
		RefreshScore ();
		nameField.text = "";
		asalField.text = "";
		backToPlay.SetActive (true);
		TampilkanInfo (nrpCur, imgFieldScroll);
	}

	public void Back(){
		streak.value = 0;
		nameField.text = "";
		asalField.text = "";
		passes.value = 3;
		SaveScore ();
		RefreshScore ();
	}

	public void Pass(){
		nameField.text = "";
		asalField.text = "";
		if (passes.value > 0) {
			passes.value = passes.value - 1;
		} else {
			passes.value = 3;
			streak.value = 0;
		}
		SaveScore ();
		RefreshScore ();
		GetRandom ();
	}
	public void PassPilgan(){
		nameField.text = "";
		asalField.text = "";
		if (passes.value > 0) {
			passes.value = passes.value - 1;
		} else {
			passes.value = 3;
			streak.value = 0;
		}
		SaveScore ();
		RefreshScore ();
		GetRandomPilgan ();
	}

	public void RefreshScore(){
		StopCoroutine ("RefreshScoreCor");
		StartCoroutine ("RefreshScoreCor");
	}

	public IEnumerator RefreshScoreCor(){
		while (!ready) {
			yield return new WaitForEndOfFrame ();
		}
		IDataReader read = db.BasicQuery ("SELECT COUNT(*) FROM TabelAngkatan WHERE Score >= 100");
		read.Read ();
		int hafal = read.GetInt32 (0);
		read.Close ();
		scoreText.text = "Pass\t\t: " + passes.value + "\tScore\t: " + score.value.ToString ()
			+ "\nCombo\t: "+ streak.value.ToString ()+ "\tHafal\t: "+ hafal + "/253";
	}

	public void LoadScore(){
		string scoreString = PlayerPrefs.GetString("score");
		string streakString = PlayerPrefs.GetString("streak");
		string passString = PlayerPrefs.GetString ("pass");
		int salt2 = PlayerPrefs.GetInt ("salt2");
		int salt1 = PlayerPrefs.GetInt ("salt1") - salt2;

		MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider ();
		byte[] bytes = md5.ComputeHash (new UTF8Encoding ().GetBytes (scoreString + salt1 + "yeah2570"));
		string encoded = BitConverter.ToString (bytes).Replace ("-", string.Empty).ToLower ();
		bytes = md5.ComputeHash (new UTF8Encoding ().GetBytes (scoreString + "ye2570ah" + salt2));
		string encoded2 = BitConverter.ToString (bytes).Replace ("-", string.Empty).ToLower ();
		bytes = md5.ComputeHash (new UTF8Encoding ().GetBytes (passString + "2570yeah" + (salt1 + salt2)));
		string encoded3 = BitConverter.ToString (bytes).Replace ("-", string.Empty).ToLower ();

		string savedMd5 = PlayerPrefs.GetString ("md5");
		string savedMd52 = PlayerPrefs.GetString ("md52");
		string savedMd53 = PlayerPrefs.GetString ("md53");
		if (savedMd5 != encoded || savedMd52 != encoded2 || savedMd53 != encoded3) {
			SaveScore ();
		} else {
			score.value = long.Parse(scoreString) - salt2 + 2 * salt1;
			streak.value = int.Parse (streakString) - salt1 + 2 * salt2;
			passes.value = int.Parse (passString) + salt1 + salt2;
		}
		RefreshScore ();
	}

	public void SaveScore(){
		int salt1 = UnityEngine.Random.Range (-2500, 1000);
		int salt2 =  UnityEngine.Random.Range (-1000, 2500);
		string scoreString = (score.value - 2 * salt1 + salt2).ToString ();
		string streakString = (streak.value - 2 * salt2 + salt1).ToString ();
		string passString = (passes.value - salt2 - salt1).ToString ();
		PlayerPrefs.SetString ("score", scoreString);
		PlayerPrefs.SetString ("streak", streakString);
		PlayerPrefs.SetString ("pass", passString);
		PlayerPrefs.SetInt ("salt1", salt1 + salt2);
		PlayerPrefs.SetInt ("salt2", salt2);
		MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider ();
		byte[] bytes = md5.ComputeHash (new UTF8Encoding ().GetBytes (scoreString + salt1 + "yeah2570"));
		string encoded = BitConverter.ToString (bytes).Replace ("-", string.Empty).ToLower ();
		PlayerPrefs.SetString ("md5", encoded);
		bytes = md5.ComputeHash (new UTF8Encoding ().GetBytes (scoreString + "ye2570ah" + salt2));
		encoded = BitConverter.ToString (bytes).Replace ("-", string.Empty).ToLower ();
		PlayerPrefs.SetString ("md52", encoded);
		bytes = md5.ComputeHash (new UTF8Encoding ().GetBytes (passString + "2570yeah" + (salt1 + salt2)));
		encoded = BitConverter.ToString (bytes).Replace ("-", string.Empty).ToLower ();
		PlayerPrefs.SetString ("md53", encoded);
	}

	struct SearchResult {
		public long nrp;
		public string nama;
	}

	public void Cari(){
		//StopCoroutine("CariCor");
		//StartCoroutine ("CariCor");
		CariActual();
	}

	public IEnumerator CariCor(){
		while (!ready) {
			yield return new WaitForEndOfFrame ();
		}
		CariActual ();
	}

	public void CariActual(){
		string searchQuery = searchField.text;
		if (!string.IsNullOrEmpty(searchQuery) && searchQuery != " " && searchQuery != prevSearch){
			int childCount = searchResults.childCount;
			for (int a = 0; a < childCount; a++){
				Destroy(searchResults.GetChild(a).gameObject);
			}

			string[] searchArgs = searchQuery.ToLower().Split(',', ' ');
			List<SearchResult> results = new List<SearchResult> ();
			HashSet<long> keys = new HashSet<long>();
			foreach (string arg in searchArgs) {
				string query = "SELECT NRP, Nama FROM TabelAngkatan WHERE "
				               + " Nama LIKE '%" + arg + "%' "
				               + " OR Panggilan LIKE '%" + arg + "%' "
				               + " OR CAST(NRP as TEXT) LIKE '%" + arg + "%' "
				               + " OR Daerah_Asal LIKE '%" + arg + "%' "
				               + " OR Jabatan LIKE '%" + arg + "%' "
				               + " OR Nomor_Telepon LIKE '%" + arg + "%' "
				               + "ORDER BY " + sorting [sortDropdown.value];
				mReader = db.BasicQuery (query);
				while (mReader.Read()){
					long res = mReader.GetInt64(0);
					if (!keys.Contains(res)){
						keys.Add(res);
						results.Add (new SearchResult(){nrp = res, nama = mReader.GetString(1)});
					}
				}
			}
			//results = results.OrderBy(x=>x.nama).ToList();
			int count = results.Count;
			for (int a = 0; a < count; a++){
				Transform trans = ((GameObject)Instantiate(Resources.Load ("SearchEntry"))).transform;
				trans.SetParent(searchResults, false);
				SearchEntry entry = trans.GetComponent<SearchEntry>();
				entry.nrp = results[a].nrp;
				entry.main = this;
				entry.text.text = results[a].nama;
			}
			searchResultsRect.verticalNormalizedPosition = 1;
			prevSearch = searchQuery;
		}
	}

	public void TampilkanInfo(long nrp, MyScrollRect rect = null){
		playPilganPanel.SetActive (false);
		playPanel.SetActive (false);
		panelCari.SetActive (false);
		infoPanel.SetActive (true);
		nrpCur = nrp;
		string query = "SELECT NRP, Nama, Panggilan, Daerah_Asal, Jabatan, Nomor_Telepon FROM TabelAngkatan WHERE NRP=" + nrp + " LIMIT 1";
		mReader = db.BasicQuery (query);
		if (mReader.Read ()) {
			infoText.text = " - Nama :\n" + mReader.GetString(1)
				+ "\n - NRP :\n" + mReader.GetInt64(0).ToString()
				+ ((mReader.IsDBNull(2) || string.IsNullOrEmpty(mReader.GetString(2))) ? "" : ("\n - Panggilan :\n" + mReader.GetString(2)))
				+ ((mReader.IsDBNull(3) || string.IsNullOrEmpty(mReader.GetString(3))) ? "" :("\n - Asal :\n" + mReader.GetString(3)))
				//+ "\n - Jabatan :\n" + ((mReader.IsDBNull(4) || string.IsNullOrEmpty(mReader.GetString(4))) ? "Anggota" : mReader.GetString(4))
				+  ((mReader.IsDBNull(4) || string.IsNullOrEmpty(mReader.GetString(4))) ? "" : ("\n - Jabatan :\n" + mReader.GetString(4)))
				+ ((mReader.IsDBNull(5) || string.IsNullOrEmpty(mReader.GetString(5))) ? "" : ("\n - No. Telepon :\n" + mReader.GetString(5)));
			if (rect == null) {
				//infoImage.texture = noImage;
				//StartCoroutine (LoadFotoCor (nrp, infoImage));
				infoImage.texture = LoadFoto(nrp);
				infoImageScroll.normalizedPosition = scrollCenter;
				SetScale (infoImage);
			} else {
				infoImageScroll.Copy (rect);
			}
		} else {
			infoImage.texture = noImage;
			infoText.text = "Record not found. NRP : " + nrp;
		}
		infoTextScroll.verticalNormalizedPosition = 1;
		mReader.Close ();
	}

	public void SetScale(RawImage img){
		float yScale = (0.8f * img.texture.height) / img.texture.width;
		if (yScale < 1)
			img.rectTransform.localScale = new Vector3 (1.0f/yScale, 1);
		else
			img.rectTransform.localScale = new Vector3 (1, yScale);
	}

	public IEnumerator LoadFotoCor(long nrp, RawImage ret){
		if (imageCache.ContainsKey (nrp)) {
			Debug.Log ("Loading cached image");
			if (nrpCur == nrp)
				ret.texture = imageCache [nrp];
			yield break;
		}
		string path;
		if (Application.platform == RuntimePlatform.Android)
			path = "jar:file://" +  Application.dataPath + "!/assets/" + nrp.ToString() + ".jpg";
		else
			path = "file://" + Application.streamingAssetsPath + Path.AltDirectorySeparatorChar + nrp.ToString() + ".jpg";

		WWW loadImg = new WWW (path);
		while (!loadImg.isDone) {
			yield return new WaitForEndOfFrame ();
		}
		long bytes = loadImg.bytes.LongLength;
		if (bytes > 0) {
			Texture2D tex = loadImg.texture;
			Debug.Log ("Loaded image, bytes : " + bytes);
			if (!imageCache.ContainsKey(nrp))
			imageCache.Add (nrp, tex);
			if (nrpCur == nrp)
				ret.texture = tex;
			yield break;
		} 
		Debug.Log ("Image not found. Path : " + path);
		if (!imageCache.ContainsKey(nrp))
			imageCache.Add (nrp, noImage);
			if (nrpCur == nrp)
				ret.texture = noImage;
		
	}
	public Texture LoadFoto(long nrp){
		if (imageCache.ContainsKey (nrp)) {
			Debug.Log ("Loading cached image");
			return imageCache [nrp];
		}
		string path;
		if (Application.platform == RuntimePlatform.Android)
			path = "jar:file://" +  Application.dataPath + "!/assets/" + nrp.ToString() + ".jpg";
		else
			path = "file://" + Application.streamingAssetsPath + Path.AltDirectorySeparatorChar + nrp.ToString() + ".jpg";

		WWW loadImg = new WWW (path);
		while (!loadImg.isDone) {
		}
		long bytes = loadImg.bytes.LongLength;
		if (bytes > 0) {
			Texture2D tex = loadImg.texture;
			Debug.Log ("Loaded image, bytes : " + bytes);
			if (!imageCache.ContainsKey(nrp))
				imageCache.Add (nrp, tex);
			return tex;
		} 
		Debug.Log ("Image not found. Path : " + path);
		if (!imageCache.ContainsKey(nrp))
			imageCache.Add (nrp, noImage);
		return noImage;

	}

	void OnDestroy()
	{
		db.CloseDB ();
	}

	/*
	
	#region Query Values
	
	/// <summary>
	/// Quick method to show how you can query everything.  Expland on the query parameters to limit what you're looking for, etc.
	/// </summary>
	public void GetAllWords()
	{
		StringBuilder sb = new StringBuilder();
		
		mConnection.Open();
		
		// if you have a bunch of stuff, this is going to be inefficient and a pain.  it's just for testing/show
		mCommand.CommandText = "SELECT * FROM " + SQL_TABLE_NAME;
		mReader = mCommand.ExecuteReader();
		while (mReader.Read())
		{
			// reuse same stringbuilder
			
			sb.Length = 0;
			sb.Append(mReader.GetString(0)).Append(" ");
			sb.Append(mReader.GetString(1)).Append(" ");
			sb.AppendLine();
			
			// view our output
			if (DebugMode)
				Debug.Log(sb.ToString());
		}
		mReader.Close();
		mConnection.Close();
	}
	
	/// <summary>
	/// Basic get, returning a value
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public string GetDefinitation(string value)
	{
		return QueryString(COL_DEFINITION, value);
	}
	
	/// <summary>
	/// Supply the column and the value you're trying to find, and it will use the primary key to query the result
	/// </summary>
	/// <param name="column"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public string QueryString(string column, string value)
	{
		string text = "Not Found";
		mConnection.Open();
		mCommand.CommandText = "SELECT " + column + " FROM " + SQL_TABLE_NAME + " WHERE " + COL_WORD + "='" + value + "'";
		mReader = mCommand.ExecuteReader();
		if (mReader.Read())
			text = mReader.GetString(0);
		else
			Debug.Log("QueryString - nothing to read...");
		mReader.Close();
		mConnection.Close();
		return text;
	}
	
	#endregion
	
	#region Update / Replace Values
	/// <summary>
	/// A 'Set' method that will set a column value for a specific player, using their name as the unique primary key
	/// to some value.  This currently just uses 'int' types, but you could modify this to use/do most anything.
	/// Remember strings need single/double quotes around their values
	/// </summary>
	/// <param name="value"></param>
	public void SetValue(string column, int value, string wordKey)
	{
		ExecuteNonQuery("UPDATE OR REPLACE " + SQL_TABLE_NAME + " SET " + column + "='" + value + "' WHERE " + COL_WORD + "='" + wordKey + "'");
	}
	
	#endregion
	
	#region Delete
	
	/// <summary>
	/// Basic delete, using the name primary key for the 
	/// </summary>
	/// <param name="wordKey"></param>
	public void DeleteWord(string wordKey)
	{
		ExecuteNonQuery("DELETE FROM " + SQL_TABLE_NAME + " WHERE " + COL_WORD + "='" + wordKey + "'");
	}
	#endregion
	
	/// <summary>
	/// Basic execute command - open, create command, execute, close
	/// </summary>
	/// <param name="commandText"></param>
	public void ExecuteNonQuery(string commandText)
	{
		mConnection.Open();
		mCommand.CommandText = commandText;
		mCommand.ExecuteNonQuery();
		mConnection.Close();
	}*/
	
	/// <summary>
	/// Clean up everything for SQLite
	/// </summary>
}
