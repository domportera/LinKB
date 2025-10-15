
### To-do
- include two default configurations in the build - "normal", "normal-split" and my preferred split layout
- usage instructions
- installer
- toggle between keyboard mode and instrument mode wiith a key combination:
    - 3 corners to switch to keboard mode, 3 corners to switch back? or...
- runtime device reconnection and disconnection prompts

### Known issues
- holding down two pads that refer to the same key can lead to an early release when both are held and only one is released
- mod keys are repeated when they probably shouldnt be 
- linnstrument's center-third LED color is inconsistent in from that of the edges
- on linux, windowing throws an exception on my machine
- input hooks have not yet been tested on linux
- reassigning pads via key press sometimes makes the pad look "pressed" until touched again
- the app sometimes doesn't fully quit, which can interfere with device connection

## Nice to have
- Read caps/scroll/num-locks' states from the OS and update the LEDs accordingly 
- Support the Linnstrument-128
- support custom "home" key designation for lighting
- custom color scheme
- linnstrument as a trackpad

    