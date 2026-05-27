#include "board_compat.h"

#if BOARD_WAVESHARE_RP2350B_PLUS_W
#include "pico/cyw43_driver.h"
#endif

void board_compat_before_cyw43_init() {
#if BOARD_WAVESHARE_RP2350B_PLUS_W
#if CYW43_PIN_WL_DYNAMIC
    static uint cyw43_pin_array[CYW43_PIN_INDEX_WL_COUNT] = {
        36, // CYW43_PIN_INDEX_WL_REG_ON
        37, // CYW43_PIN_INDEX_WL_DATA_OUT
        37, // CYW43_PIN_INDEX_WL_DATA_IN
        37, // CYW43_PIN_INDEX_WL_HOST_WAKE
        39, // CYW43_PIN_INDEX_WL_CLOCK
        38  // CYW43_PIN_INDEX_WL_CS
    };
    cyw43_set_pins_wl(cyw43_pin_array);
#endif
#endif
}
