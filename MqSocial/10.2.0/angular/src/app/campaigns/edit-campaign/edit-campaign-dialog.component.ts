import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    CampaignServiceProxy,
    CampaignDto,
    CampaignStatus,
    CompanyServiceProxy,
    CompanyDto,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import moment from 'moment';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { DatePicker } from 'primeng/datepicker';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Button } from 'primeng/button';

@Component({
    templateUrl: './edit-campaign-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        InputText,
        Textarea,
        DatePicker,
        Select,
        InputNumber,
        Button,
    ],
})
export class EditCampaignDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    campaign = new CampaignDto();
    companies: CompanyDto[] = [];
    id: string;

    startDate: Date | null = null;
    endDate: Date | null = null;

    campaignStatuses = [
        { value: CampaignStatus.Draft, label: 'Draft' },
        { value: CampaignStatus.Active, label: 'Active' },
        { value: CampaignStatus.Paused, label: 'Paused' },
        { value: CampaignStatus.Completed, label: 'Completed' },
    ];

    constructor(
        injector: Injector,
        public _campaignService: CampaignServiceProxy,
        public _companyService: CompanyServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this._companyService.getAll(undefined, undefined, 0, 1000).subscribe((companies) => {
            this.companies = companies.items;
            this._campaignService.get(this.id).subscribe((result) => {
                this.campaign = result;
                this.startDate = result.startDate ? result.startDate.toDate() : null;
                this.endDate = result.endDate ? result.endDate.toDate() : null;
                this.cd.detectChanges();
            });
        });
    }

    save(): void {
        if (this.campaign.status === CampaignStatus.Completed) {
            abp.message.confirm(
                'Tất cả Contract và ContractKol liên quan sẽ được chuyển sang trạng thái Hoàn thành. Bạn có chắc chắn?',
                'Hoàn tất Campaign',
                (confirmed: boolean) => {
                    if (confirmed) this._doSave();
                }
            );
        } else {
            this._doSave();
        }
    }

    private _doSave(): void {
        this.saving = true;
        this.campaign.startDate = this.startDate ? moment(this.startDate) : undefined;
        this.campaign.endDate = this.endDate ? moment(this.endDate) : undefined;

        this._campaignService.update(this.campaign).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
