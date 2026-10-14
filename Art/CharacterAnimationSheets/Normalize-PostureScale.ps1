function Set-PostureScale($clips, $key) {
 # A bent pose is shorter in the artwork already. Preserve the standing pixel
 # density instead of squeezing its complete bounds into the collider height.
 $standing = $clips[5]
 foreach($row in @(3,4)) {
  $clip = $clips[$row]
  $sourceScale = $clip.textureHeight / $standing.textureHeight
  # Neon crouch is a separate high-resolution strip. Its helmet is 2.25x
  # the pixel size of the helmet on the 1254px standing reference sheet.
  if($key -eq '01-neon-runner' -and $row -eq 3) {
   $sourceScale = 2.25 * ($clip.textureWidth / 2172) / ($standing.textureHeight / 1254)
  }
  $clip.pixelsPerUnit = $standing.pixelsPerUnit * $sourceScale
 }
}
