WaterXNA
========
Fork of https://github.com/Noxalus/WaterXNA

Lake water rendering project with XNA (C#/DirectX 9).

Video demonstration can be seen here: http://www.youtube.com/watch?v=VJmNeEJY-Ss

![WaterXNA](https://dl.dropboxusercontent.com/u/63123790/screenshots/Projet%203D/waterXNA.jpg)

# Controls

## Keyboard:

### Move camera
* W => Go forward
* S => Go backward
* A => Strafe left
* D => Strafe right
* Hold Right Mouse Button and Move Mouse => Rotate

### Show/Hide scene parts
* F1 => Show/Hide some debug informations
* F2 => Switch between wireframe and solid mode
* F3 => Turn on/off lighting
* F4 => Show/Hide water plane
* F5 => Show/Hide refraction
* F6 => Show/Hide reflection
* F7 => Enable/Disable Fresnel term computation
* F8 => Enable/Disable waves (ripples)
* F9 => Enable/Disable specular lighting onto water plane
* F10 => Show/Hide skybox

### Edit parameters
* Insert/Delete => Increase/Decrease ambient lighting intensity
* Home/End => Increase/Decrease diffuse lighting intensity
* Page Up/Page Down => Increase/Decrease water level
* W/X => Change water scale
* C/V => Change refraction/reflection merge term when Fresnel is disabled
* B/N => Change waves velocity

### Numpad:
* 8/5 => Change Z direction of sun light 
* 4/6 => Change X direction of sun light
* 9/3 => Change Y direction of sun light

## Mouse:
Control the camera direction.

## Building from Source Code
1. Clone following projects:
  * This one.
  * https://github.com/FNA-XNA/FNA
  * https://github.com/FontStashSharp/FontStashSharp
  * https://github.com/rds1983/XNAssets
  All repos must present on the same folder level like this:
  ![image](https://github.com/rds1983/WaterXNA/assets/1057289/9371de04-31bb-426a-b73b-52e7160390fa)

2. Open Water.FNA.Core.sln in the IDE.

