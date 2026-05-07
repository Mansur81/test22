function parsePrices(input) {
  return input
    .split(/[\n,\s]+/)
    .map((value) => Number(value.trim()))
    .filter((value) => Number.isFinite(value));
}

function sma(values, period) {
  if (values.length < period) {
    return null;
  }

  const window = values.slice(-period);
  return window.reduce((sum, value) => sum + value, 0) / period;
}

function rsi(values, period) {
  if (values.length <= period) {
    return null;
  }

  let gains = 0;
  let losses = 0;

  for (let i = values.length - period; i < values.length; i += 1) {
    const diff = values[i] - values[i - 1];
    if (diff > 0) gains += diff;
    else losses -= diff;
  }

  if (losses === 0) return 100;
  const rs = gains / losses;
  return 100 - 100 / (1 + rs);
}

function generateSignal(fastSma, slowSma, currentRsi) {
  if (fastSma === null || slowSma === null || currentRsi === null) {
    return { trend: 'Insufficient data', signal: 'WAIT', note: 'Add more prices for selected periods.' };
  }

  if (fastSma > slowSma && currentRsi < 70) {
    return {
      trend: 'Uptrend',
      signal: 'BUY',
      note: 'Momentum is positive (fast SMA above slow SMA) and RSI is not overbought.'
    };
  }

  if (fastSma < slowSma && currentRsi > 30) {
    return {
      trend: 'Downtrend',
      signal: 'SELL',
      note: 'Momentum is negative (fast SMA below slow SMA) and RSI is not oversold.'
    };
  }

  return {
    trend: fastSma >= slowSma ? 'Weak uptrend' : 'Weak downtrend',
    signal: 'HOLD',
    note: 'Trend exists, but RSI suggests caution.'
  };
}

function renderAnalysis() {
  const prices = parsePrices(document.getElementById('prices').value);
  const fastPeriod = Number(document.getElementById('fastPeriod').value);
  const slowPeriod = Number(document.getElementById('slowPeriod').value);
  const rsiPeriod = Number(document.getElementById('rsiPeriod').value);

  if (fastPeriod >= slowPeriod) {
    document.getElementById('message').textContent = 'Fast SMA period should be smaller than slow SMA period.';
    return;
  }

  const fastSma = sma(prices, fastPeriod);
  const slowSma = sma(prices, slowPeriod);
  const currentRsi = rsi(prices, rsiPeriod);
  const analysis = generateSignal(fastSma, slowSma, currentRsi);

  const signalEl = document.getElementById('signalValue');
  signalEl.className = analysis.signal.toLowerCase();

  document.getElementById('trendValue').textContent = analysis.trend;
  document.getElementById('signalValue').textContent = analysis.signal;
  document.getElementById('fastSmaValue').textContent = fastSma?.toFixed(5) ?? '-';
  document.getElementById('slowSmaValue').textContent = slowSma?.toFixed(5) ?? '-';
  document.getElementById('rsiValue').textContent = currentRsi?.toFixed(2) ?? '-';
  document.getElementById('message').textContent = analysis.note;
}

document.getElementById('analyzeBtn').addEventListener('click', renderAnalysis);
renderAnalysis();
