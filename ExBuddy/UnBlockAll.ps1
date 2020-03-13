# Load assembly
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms")

# Test Pathing
if (Test-Path ..\..\Plugins\ExBuddy\Agents) {
  # Path Correct Unblock all and confirm success to user
  Get-ChildItem -Path '..\ExBuddy' -Recurse | Unblock-File
  [System.Windows.Forms.MessageBox]::Show("Exbuddy was correctly installed and is now UnBlocked","Success",[System.Windows.Forms.MessageBoxButtons]::OK,[System.Windows.Forms.MessageBoxIcon]::Asterisk)
}
else {
  # Path Incorrect Warn User and exit
  [System.Windows.Forms.MessageBox]::Show("It seems as though Exbuddy has not been extracted into the correct folder, path should be <rbfolder>\Plugins\Exbuddy","Error",[System.Windows.Forms.MessageBoxButtons]::OK,[System.Windows.Forms.MessageBoxIcon]::Error)
}
