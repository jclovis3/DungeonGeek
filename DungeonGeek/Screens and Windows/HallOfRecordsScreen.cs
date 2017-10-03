using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DungeonGeek
{
    static class HallOfRecordsScreen
    {
        internal struct GameRecord
        {
            internal bool Authentic
            {get { return MakeHash(this).SequenceEqual(MD5Hash);}}
            internal string PlayerName;
            internal int Level;
            internal string KilledBy;
            internal int GoldScore;
            internal bool PlayerDied; // If player wins the game, this should be noted differently in the records.
            internal byte[] MD5Hash; // 16 byte array

        }

        #region Fields
        private static GameWindow window = new GameWindow();
        private static List<string> recordText;
        private static string header = "Hall of Records";
        private static string instructions = "Esc - Exit";
        private static List<GameRecord> records;
        private static int numberOfRecordsAllowed = 10;
        private static bool dataLoaded = false;
        private static bool dataSaved = false;
        private static bool firstOpenedWindow = true;



        #endregion


        private static byte[] MakeHash(GameRecord openRecord)
        {
            // https://support.microsoft.com/en-us/help/307020/how-to-compute-and-compare-hash-values-by-using-visual-c
            string sSourceData;
            byte[] tmpSource;
            byte[] tmpHash;

            // Used array of bytes to prevent string literals from being present in the compiled version
            // This 60 byte key was randomly generated
            byte[] privateKey = new byte[]
            {
                37,22,45,112,159,107,80,164,136,86,166,218,110,117,231,124,176,113,
                222,113,173,157,194,246,193,125,125,112,175,67,123,208,70,84,75,86,
                148,66,35,28,171,125,123,33,244,168,153,255,200,147,14,236,146,29,
                187,160,128,161,53
            };
            StringBuilder pKey = new StringBuilder();
            foreach (var value in privateKey)
                pKey.Append((char)value);

            sSourceData = openRecord.PlayerName +
                openRecord.Level.ToString() +
                (openRecord.PlayerDied?"Died":"Won") +
                openRecord.KilledBy +
                openRecord.GoldScore.ToString() +
                pKey.ToString();
            //Create a byte array from source data.
            tmpSource = Encoding.ASCII.GetBytes(sSourceData);

            //Compute hash based on source data. -- "Note that to compute another hash value, you will need to create another instance of the class"
            tmpHash = new MD5CryptoServiceProvider().ComputeHash(tmpSource);
            // "The tmpHash byte array now holds the computed hash value (128-bit value=16 bytes) for your source data"
            return tmpHash;

        }

        private static bool RecordAuthentic(GameRecord sealedRecord)
        {
            if (sealedRecord.MD5Hash == MakeHash(sealedRecord))
                return true;
            else return false;
        }

        private static GameRecord SealRecord(GameRecord openRecord)
        {
            openRecord.MD5Hash = MakeHash(openRecord);
            return openRecord;
        }

        internal static void Initialize(GraphicsDevice gd)
        {
            recordText = new List<string>();
            window.Initialize(gd, recordText, header, instructions);
            GetRecords();
        }

        /// <summary>
        /// Opens the hall of records file, attempts to read in records, and validates them against
        /// their MD5 checksum. If the record is authentic, it is added to the records list in memory.
        /// </summary>
        private static void GetRecords()
        {
            records = new List<GameRecord>();
            try
            {
                using (FileStream stream = new FileStream(GameConstants.FILE_SCORES, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            try
                            {
                                var newRecord = new GameRecord();
                                newRecord.PlayerName = reader.ReadString();
                                newRecord.Level = reader.ReadInt32();
                                newRecord.KilledBy = reader.ReadString();
                                newRecord.GoldScore = reader.ReadInt32();
                                newRecord.PlayerDied = reader.ReadBoolean();
                                newRecord.MD5Hash = reader.ReadBytes(16);

                                // Check that record is authentic
                                if (newRecord.Authentic)
                                    records.Add(newRecord);
                            }
                            catch (IOException) { }
                            catch (ObjectDisposedException) { }

                        }
                        reader.Close(); // BinaryReader object wraps on the FileStream object and closes it too
                    }
                }

            }
            catch (IOException)
            {
                // If file not found, a blank list will be used and the hall of records will be
                // empty. The next save action will create the file. While eating an exception is
                // usually regarded as a bad idea, this particular exception is handled by not attempting
                // to read the file and ignoring the error. No need to pass it up to the user.

            }

            dataLoaded = true;
            dataSaved = true;
            UpdateRecordText();
        }

        /// <summary>
        /// Writes each record to a binary file, replacing any existing file of the same name.
        /// </summary>
        private static void SaveRecords()
        {
            // Overwrites existing file with new list of records
            try
            {
                using (FileStream stream = new FileStream(GameConstants.FILE_SCORES, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        foreach (var record in records)
                        {
                            writer.Write(record.PlayerName);
                            writer.Write(record.Level);
                            writer.Write(record.KilledBy);
                            writer.Write(record.GoldScore);
                            writer.Write(record.PlayerDied);
                            writer.Write(record.MD5Hash);
                        }
                        writer.Close(); // BinaryWriter object wraps on the FileStream object and closes it too
                        dataSaved = true;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Unable to record hall of records. Please check file and directory permissions" +
                    " then close hall of records and reopen it again to attempt saving data.");
                dataSaved = false;
            }
        }

        /// <summary>
        /// Checks to see if the game that ended with the provided results was good enough to make
        /// it to the hall of records. If it does, the record is added and the bottom record dropped.
        /// </summary>
        /// <param name="playerName">Name player gave in the beginning of the game</param>
        /// <param name="floorLevel">The floor number the hero died at</param>
        /// <param name="playerDied">Marks the victory condition of the player</param>
        /// <param name="killedBy">How the player died is described here</param>
        /// <param name="goldScore">The score to compare all players by (after playerDied of course)</param>
        internal static void QualifyResults(string playerName, int floorLevel, bool playerDied, string killedBy, int goldScore)
        {

            if (!dataLoaded) GetRecords();

            // Generate the new record
            GameRecord thisRecord = new GameRecord
            {
                PlayerName = playerName,
                Level = floorLevel,
                PlayerDied = playerDied,
                KilledBy = killedBy,
                GoldScore = goldScore,
                MD5Hash = new byte[16]
            };



            // Add the record, then sort the list by PlayerDied and then GoldScore
            // Note, sorting places false before true
            records.Add(SealRecord(thisRecord));
            records = records.OrderBy(x => x.PlayerDied).ThenByDescending(x => x.GoldScore).ToList();

            // Drop anything past the authorized limit
            if (records.Count > numberOfRecordsAllowed)
                records.RemoveRange(numberOfRecordsAllowed, records.Count - numberOfRecordsAllowed);

            SaveRecords();
            UpdateRecordText();

        }

        private static void UpdateRecordText()
        {
            recordText.Clear();
            for(int i=0; i<records.Count; i++)
            {
                // Sample text:
                // Hagar the Horrible killed by a rat on level 1 with 5 Au
                // Joe the programmer won the game on level 150 with 987,123 Au"

                recordText.Add(string.Format(
                    "{0} {1} with {2:n0} Au",
                    records[i].PlayerName,
                    (
                        records[i].PlayerDied ?
                        records[i].KilledBy + " on level " + records[i].Level :
                        "won the game"
                    ),
                    records[i].GoldScore
                ));
            }
        }


        internal static void Draw(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            if (!dataLoaded) GetRecords();
            if (firstOpenedWindow && !dataSaved) SaveRecords();
            firstOpenedWindow = false;
            window.Draw(spriteBatch, viewPortBounds);
        }

        internal static bool ProcessPlayerInputs(Keys key)
        {
            if (key == Keys.Escape)
            {
                firstOpenedWindow = true;
                return true;
            }

            return false; // Does not allow window to exit yet
        }





    }
}
