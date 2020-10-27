# EECS3481-Course-Project

Project by: 

Mykola Slobodyanyuk

Huan Ho

Farnad Kazem-zadeh

Jaiveer Singh

Rui Yang Huang


Code examples/explanations:

This section is from: Program.cs 

The purpose of all this additional code is so that everysingle file in the folder will be locked with a unique password, so if anyone tries to bruteforce or preform any kind of recovery on the encrypted data they will have to decrypt each file indepantly of each other, sicne each has new password. THe passwords them selves are stored in a File which the code it self creates and writes to, called: "autopasswords". this file is unencrypted in our version for ease of showing the code working, but can also easly be encrypted with its own password at the end, aswell as hidden, and even be sent back to "master control machine" and deleted of the local machine (if this was malware). 

From (lines 165-204):

            private static void SelectorForAlgo( string chosenFile, string chosenAlgo, string chosenKey, string chosenAction){

            if (chosenKey.Equals(choiceAuto)){

                  if(chosenAction.Equals(choiceEncrypt)){

                    using (StreamWriter file = File.AppendText(currentDir+autokeyFileLocation)){

                      if(chosenAlgo.Equals(choiceAlgoAES)){
                        ranIVgen = GetRandomAlphaNumeric(4);
                        ranPasswordgen = GetRandomAlphaNumeric(8);
                        file.WriteLine(ranPasswordgen +"="+ ranIVgen);
                      }else{
                        ranPasswordgen = GetRandomAlphaNumeric(8);
                        file.WriteLine(ranPasswordgen);
                      }
                    }
                  }else if (chosenAction.Equals(choiceDecrypt)){


                      autoDecryptString = autoDecryptLine[autoDecryptCounter];
                      autoDecryptCounter = (autoDecryptCounter + 1);


                    if(chosenAlgo.Equals(choiceAlgoAES)){
                     autoDecryptLineAES = autoDecryptString.Split('=');
                     ranIVgen = autoDecryptLineAES[1];
                     ranPasswordgen = autoDecryptLineAES[0];

                    }else{
                      ranPasswordgen = autoDecryptString;

                    }
                  }else{

                  }
            }else{
              ranPasswordgen = chosenKey;
            }
            
 This code snippet shows how we edit the file that we use for storing the passwords and IV keys (for AES), and how we extract the keys, the creation and rewriting of the file is done earlier on at lines(55-62):    
   
                if (srcKey.Equals(choiceAuto)){
                currentDir = Directory.GetCurrentDirectory();
                if(File.Exists(currentDir+autokeyFileLocation)){

                }else{
                  using (StreamWriter sw = File.CreateText(currentDir+autokeyFileLocation))  { }
                    Console.WriteLine("File created: "+ autokeyFileLocation);
                }
                
and lines (114-123):

            srcAction = Console.ReadLine();
            if ((srcAction.Equals(choiceDecrypt))|(srcAction.Equals(choiceEncrypt))){
                choiceLoopOut = 0;
                if (srcAction.Equals(choiceDecrypt)){
                  autoDecryptLine = File.ReadAllLines(currentDir+autokeyFileLocation);
                }else{
                  File.Delete(currentDir+autokeyFileLocation);
                  using (StreamWriter sw = File.CreateText(currentDir+autokeyFileLocation))  { }
                //  Console.WriteLine("File rewritten: "+ autokeyFileLocation);
                } 
                
  The creation of the file will always occur since it uses a path that it knows will always exist. 

As for the AES: 
           
           I (Mykola) made a mistake in  their description so: 

AES/CTR: ProcessFile

AES3481: encryption, decrytion
                        
