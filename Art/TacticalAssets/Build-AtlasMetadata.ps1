Add-Type -AssemblyName System.Drawing
$root=Join-Path $PSScriptRoot '../../Assets/ClimbBattlePrototype/Resources/ClimbBattleArt/Tactical'
$names=@('Base','Barrel','Cover','TrapA','TrapB','Switch','EffectA','EffectB','Ammo')
foreach($key in @('NeonCity','IndustrialLava','CollapseTower','EgyptPyramid')) {
 $bitmap=[System.Drawing.Bitmap]::FromFile((Join-Path $root "$key.png"))
 $frames=@()
 for($i=0;$i -lt 9;$i++) {
  $row=[math]::Floor($i/3);$col=$i%3
  $xs=if($row -eq 0){@(0,400,837,1254)}elseif($row -eq 1){@(0,423,889,1254)}else{@(0,410,872,1254)}
  $ys=@(0,420,815,1254)
  $minX=$bitmap.Width;$minY=$bitmap.Height;$maxX=-1;$maxY=-1
  for($x=[int]($xs[$col]*$bitmap.Width/1254);$x -lt [int]($xs[$col+1]*$bitmap.Width/1254);$x++) {
   for($y=[int]($ys[$row]*$bitmap.Height/1254);$y -lt [int]($ys[$row+1]*$bitmap.Height/1254);$y++) {
    if($bitmap.GetPixel($x,$y).A -gt 80){$minX=[math]::Min($minX,$x);$minY=[math]::Min($minY,$y);$maxX=[math]::Max($maxX,$x);$maxY=[math]::Max($maxY,$y)}
   }
  }
  if($maxX -lt $minX){throw "Missing sprite $key/$($names[$i])"}
  $frames+=@{name=$names[$i];x=$minX;y=$bitmap.Height-1-$maxY;width=$maxX-$minX+1;height=$maxY-$minY+1}
 }
 @{width=$bitmap.Width;height=$bitmap.Height;frames=$frames} | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $root "$key.json")
 $bitmap.Dispose()
 Write-Output "$key : nine sprite regions registered"
}
