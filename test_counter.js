// Simple test script to verify the counter functionality
const puppeteer = require('puppeteer');

(async () => {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  
  try {
    console.log('Navigating to counter...');
    await page.goto('http://localhost:5150/counter', { waitUntil: 'networkidle0' });
    
    // Check if page loaded
    const title = await page.title();
    console.log('Page title:', title);
    
    // Check initial counter value
    const initialValue = await page.$eval('h1', el => el.textContent);
    console.log('Initial counter:', initialValue);
    
    // Click increment button
    console.log('Clicking increment button...');
    await page.click('button.btn-primary');
    await page.waitForTimeout(500); // Wait for update
    
    // Check new counter value  
    const newValue = await page.$eval('h1', el => el.textContent);
    console.log('After increment:', newValue);
    
    // Click decrement button
    console.log('Clicking decrement button...');
    await page.click('button.btn-secondary');
    await page.waitForTimeout(500);
    
    const finalValue = await page.$eval('h1', el => el.textContent);
    console.log('After decrement:', finalValue);
    
    // Verify functionality
    if (initialValue === 'Counter: 0' && newValue === 'Counter: 1' && finalValue === 'Counter: 0') {
      console.log('✅ Counter functionality working correctly!');
    } else {
      console.log('❌ Counter functionality not working as expected');
    }
    
  } catch (error) {
    console.error('Error:', error.message);
  } finally {
    await browser.close();
  }
})();