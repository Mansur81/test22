# Forex Trend Trading Bot

A lightweight frontend bot prototype that analyzes forex price trends using:
- Fast and slow simple moving averages (SMA)
- Relative Strength Index (RSI)
- Rule-based BUY / SELL / HOLD signal generation

## Usage
1. Open `index.html` in a browser.
2. Paste closing prices separated by commas/new lines.
3. Tune SMA and RSI periods.
4. Click **Analyze Trend**.

## Strategy Logic
- **BUY**: fast SMA > slow SMA and RSI < 70
- **SELL**: fast SMA < slow SMA and RSI > 30
- **HOLD**: otherwise

> This is an educational demo and not financial advice.
