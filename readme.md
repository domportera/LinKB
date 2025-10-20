
Note: Linux users need to be a part of the "input" group on their machines.
`sudo usermod -aG input YOUR_USERNAME`

### To-do
- include two default configurations in the build - "normal", "normal-split" and my preferred split layout
- usage instructions
- installer
- toggle between keyboard mode and instrument mode via the linnstrument and application
- Support for the side buttons on the linnstrument
- Support the Linnstrument-128 (in progress)
- Better GUI workflow for mapping pads to keys

### Known issues
- Currently, the device usage seems to be exclusive to this application, therefore it cannot simultaneously be used in another application (e.g. a DAW)
- holding down two pads that refer to the same key can lead to an early release when both are held and only one is released (simple to fix)
- mod keys are repeated when they probably shouldnt be 
- linnstrument's center-third LED color is inconsistent in from that of the edges (possibly specific to my device)
- on linux, windowing throws an exception on my machine
- reassigning pads via key press sometimes makes the pad look "pressed" until touched again
- Currently not thrilled with my serialization scheme (layout files) - they're somewhat human readable, but could be better structured, more pleasant to edit by hand, and more robust to adapt to future changes

## Long-term goals
- Mac OS support
  - currently this is impossible since I don't own a Mac, and am unlikely to for the foreseeable future
- Read caps/scroll/num-lock states from the OS and update the LEDs accordingly 
- support custom "home" key designation for lighting
- custom color scheme
- linnstrument as a trackpad

    
