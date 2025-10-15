
### To-do
- include two default configurations in the build - "normal", "normal-split" and my preferred split layout
- usage instructions
- installer

### Known issues
- holding down two pads that refer to the same key can lead to an early release when both are held and only one is released
- mod keys are repeated when they probably shouldnt be 
- linnstrument's center-third LED color is inconsistent in from that of the edges
- on linux, windowing throws an exception on my machine
- input hooks have not yet been tested on linux
- reassigning pads via key press sometimes makes the pad look "pressed" until touched again
- devices can fail to open due to arcane WindowsMM issues? the reliable solve seems to be unplug-and-replug, but there must be a better way. like something (this app) is holding onto the reference in unmanaged land...

## Nice to have
- Read caps/scroll/num-locks' states from the OS and update the LEDs accordingly 
- Support the Linnstrument-128
- support custom "home" key designation for lighting
- custom color scheme

    