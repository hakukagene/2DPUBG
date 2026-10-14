Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Collections.Generic;
public static class SpriteBounds {
 public static int[][] Scan(string path, double top, double bottom) {
  using(var image=new Bitmap(path)) {
   int y0=(int)(top*image.Height), y1=(int)(bottom*image.Height);
   var occupied=new bool[image.Width];
   for(int x=0;x<image.Width;x++)for(int y=y0;y<y1;y++)if(image.GetPixel(x,y).A>96){occupied[x]=true;break;}
   var runs=new List<int[]>(); int start=-1,last=-1;
   for(int x=0;x<image.Width+5;x++) {
    if(x<image.Width&&occupied[x]){if(start<0)start=x;last=x;}
    else if(start>=0&&x-last>4){if(last-start>12)runs.Add(new[]{start,last+1});start=-1;}
   }
   var bounds=new List<int[]>();
   foreach(var run in runs){int minY=y1,maxY=y0;
    for(int x=run[0];x<run[1];x++)for(int y=y0;y<y1;y++)if(image.GetPixel(x,y).A>96){minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
    bounds.Add(new[]{run[0],image.Height-1-maxY,run[1]-run[0],maxY-minY+1});
   }
   return bounds.ToArray();
  }
 }
}
'@
$root = Join-Path $PSScriptRoot '../../Assets/ClimbBattlePrototype/Resources/ClimbBattleArt/Characters/Animations'
. (Join-Path $PSScriptRoot 'Normalize-PostureScale.ps1')
$rows = @{
 '01-neon-runner' = @(0,.181,.362,.584,.727,1)
 '02-forge-guard' = @(0,.183,.357,.56,.724,.797,1)
 '03-ruin-scout' = @(0,.189,.368,.586,.731,.810,1)
 '04-dune-warden' = @(0,.189,.36,.57,.71,.80,1)
 '05-aegis-mage' = @(0,.17,.337,.522,.69,.795,1)
}
foreach($key in ($rows.Keys | Sort-Object)) {
 $ranges=$rows[$key]
 $clips=@()
 for($row=0;$row -lt 6;$row++) {
  $texture=$key
  $sourceRow=$row
  if($key -eq '01-neon-runner') {
   if($row -eq 3){$texture='01-neon-crouch';$top=0;$bottom=1}
   elseif($row -eq 4){$sourceRow=3}
   elseif($row -eq 5){$sourceRow=4}
  }
  if($texture -ne '01-neon-crouch'){$top=$ranges[$sourceRow];$bottom=$ranges[$sourceRow+1]}
  $bounds=[SpriteBounds]::Scan((Join-Path $root ($texture+'.png')),$top,$bottom)
  if($key -eq '01-neon-runner' -and $row -eq 4){$bounds=$bounds[1..5]}
  if($bounds.Count -lt 5 -or $bounds.Count -gt 6){throw "Invalid frame count $key row $row : $($bounds.Count)"}
  $heights=@($bounds | ForEach-Object {$_[3]} | Sort-Object)
  $height=$heights[[int]($heights.Count/2)]
  $worldHeight=@(1.85,1.85,1.5,1.05,.5,1.85)[$row]
  $frames=@($bounds | ForEach-Object {@{x=$_[0];y=$_[1];width=$_[2];height=$_[3]}})
  $image=[System.Drawing.Bitmap]::FromFile((Join-Path $root ($texture+'.png')))
  $clips+=@{texture=$texture;textureWidth=$image.Width;textureHeight=$image.Height;pixelsPerUnit=$height/$worldHeight;frames=$frames}
  $image.Dispose()
  Write-Output "$key row $row frames=$($frames.Count) height=$height"
 }
 Set-PostureScale $clips $key
 @{clips=$clips} | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $root ($key+'.json'))
}
