# Web Chat

## How to develop

### Starting the site

The whole site is a static HTML site at `CateringDemo/Web/public`. If you need to deploy, just XCOPY this folder to your server.

You don't need to build anything to start this site. For simplicity, everything is pre-built and committed to this repository. This includes AAF and DirectLineJS bundle.

If you don't have a static web server, we have included one. Type `npm start` and then browse to http://localhost:5000/.

### What to try out

- Type `card markdown`
   - This is serving an AC 2.0 card, which is under `public/demo-card.js`
   - This card is backed by a custom "local channel adapter", which is under `public/demo-card-channel-adapter.js`
- Type `card breakfast`
   - This card do not have `appId` set and it will be rendered using AC 1.2.5.

### Using my own bot token

Modify `public/fetch-bot-token.js`. The `fetchBotToken` function is an async function and should return the token as a string.

### Using my own version of DirectLineJS

You will need to build your own copy of `directLine.js` based on the GitHub repository.

1. Build your own copy of `directLine.js`
   - `git clone https://github.com/microsoft/BotFramework-DirectLineJS.git`
   - `cd BotFramework-DirectLineJS`
   - `npm install`
   - `npm run build`
   - When built under development mode (default), this file `dist/directLine.sj` should be about 1.7 MB
1. Copy `BotFramework-DirectLineJS/dist/directLine.js` to `CateringDemo/Web/public/external/`, overwriting existing file
   - File name is *case-sensitive*

To verify the build is being loaded.

1. To check if `public/external/directLine.js` is the correct version
   - Add `console.log('123')` to the top of the file
   - Refresh the page
   - Press <kbd>F12</kbd>, in the dev tools console tab, you should see `123`, it means your file is loaded

### Modifying the AAF Channel Adapter

> No re-build is required after modifying the channel adapter.

1. Modify `public/default-channel-adapter.js`
   - The default implementation already have access to the `directLine` object
1. Refresh the page

## How to update Adaptive Cards AAF bundle

In very rare case, you may need to update the AAF bundle.

You will need to clone Adaptive Cards repository, copy the contents, and then build.

1. Clone the repository, under the `ts/aaf` branch
   1. `git clone https://github.com/microsoft/adaptivecards.git -b ts/aaf`
1. Copy only specified trees to `CateringDemo` repository
   1. Copy from `adaptivecards/source/nodejs/adaptivecards` to `CateringDemo/Web/externals/adaptivecards`, overwriting existing files and subdirectories
   1. Copy from `adaptivecards/source/nodejs/adaptivecards-aaf` to `CateringDemo/Web/externals/adaptivecards-aaf`, overwriting existing files and subdirectories
1. Build
   1. Under `CateringDemo/Web` folder
   1. Prepare the packages
      -  If this is your first time
         1. `npm install`
      -  If you already built it once and want to re-download all packages
         1. `npm run clean`
         1. `npm run bootstrap`
      -  If you already built and think the dependencies is up-to-date, you can skip this step
   1. Then, run `npm run build`
      -  It will automatically copy build output to `CateringDemo/Web/public/externals/`

To verify, these files under `CateringDemo/Web/public/externals/` should be updated: `aaf.css`, `aaf.js`, `adaptivecards.css`.
