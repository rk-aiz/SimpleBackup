using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SimpleBackup.Helpers
{
    internal static class StorageMeasure
    {
        /*
        public StorageMeasure()
        {
        $SyncListView[$s].Collection.Clear()
        $Items = [IO.DriveInfo]::GetDrives()
        foreach ($item in $Items){
            $data = New - Object Dynamic.ExpandoObject
            $data.Tag = $s
            if ($item.VolumeLabel){
                $data.ItemName = "{0}[{2}] ({1} Free)" - f $item.Name, (ByteToString $item.AvailableFreeSpace), $item.VolumeLabel
            }else
                    {
                $data.ItemName = "{0}[Local Disk] ({1} Free)" - f $item.Name, (ByteToString $item.AvailableFreeSpace)
            }
            $data.FullName = $item.RootDirectory
            $data.Identifier = 'Drive:{0}' - f $item.RootDirectory
            $data.BaseItemName = $data.ItemName
            $data.Type = $item.DriveFormat
            $data.Extension = ""
            $data.LWT = $(Get - Item - LiteralPath $data.FullName).LastWriteTime
            $data.DataByte = [double]$item.TotalSize
            $data.Size = ByteToString $item.TotalSize
            $data.Hidden = $false
            $data.Icon = [System.Convert]::ToChar([System.Convert]::ToInt32("EDA2", 16))
            $SyncListView[$s].Collection.Add($data)
        }
        $SyncListView[$s].Mode = 'DriveInfo'
        $HiddenItemCount = 0
        if ($SyncData.BindData[$s].HiddenItemVisibleMode - eq $false) {
                    for ($i = 0; $i - lt $SyncListView[$s].Collection.Count; $i++) { if ($SyncListView[$s].Collection[$i].Hidden - eq $true) {$HiddenItemCount++} }
                }
        $SyncData.BindData[$s].StatusTextItemCount = $SyncListView[$s].Collection.Count - $HiddenItemCount
        $SyncData.BindData[$s].StatusTextHiddenItemCount = $HiddenItemCount
    } #End function DriveInfoListView
        */
    }

}
