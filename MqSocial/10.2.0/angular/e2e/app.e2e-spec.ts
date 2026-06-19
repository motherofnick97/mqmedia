import { MqSocialTemplatePage } from './app.po';

describe('MqSocial App', function () {
    let page: MqSocialTemplatePage;

    beforeEach(() => {
        page = new MqSocialTemplatePage();
    });

    it('should display message saying app works', () => {
        page.navigateTo();
        expect(page.getParagraphText()).toEqual('app works!');
    });
});
