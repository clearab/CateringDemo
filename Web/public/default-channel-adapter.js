window.App || (window.App = {});

window.App.createDefaultChannelAdapter = ({ directLine }) => {
  let hideSubsequentError;

  return {
    async sendRequestAsync(request) {
      console.log(request);
      console.log(directLine);

      !hideSubsequentError && alert('Got a request in DefaultChannelAdapter');
      hideSubsequentError = true;

      throw new Error('not implemented');
    }
  };
};
