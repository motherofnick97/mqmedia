import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    ContractServiceProxy,
    CreateContractDto,
    ContractStatus,
    CampaignServiceProxy,
    CampaignDto,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { Button } from 'primeng/button';

@Component({
    templateUrl: './create-contract-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        InputText,
        Textarea,
        Select,
        InputNumber,
        Button,
    ],
})
export class CreateContractDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    contract = new CreateContractDto();
    campaigns: CampaignDto[] = [];

    contractStatuses = [
        { value: ContractStatus.Prepare, label: 'Chuẩn bị' },
        { value: ContractStatus.Processing, label: 'Đang triển khai' },
        { value: ContractStatus.Complete, label: 'Hoàn thành' },
        { value: ContractStatus.Pause, label: 'Tạm dừng' },
        { value: ContractStatus.Cancel, label: 'Hủy' },
    ];

    constructor(
        injector: Injector,
        public _contractService: ContractServiceProxy,
        private _campaignService: CampaignServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.contract.status = ContractStatus.Prepare;
        this._campaignService.getAll(undefined, undefined, undefined, 0, 1000).subscribe((result) => {
            this.campaigns = result.items ?? [];
            this.cd.detectChanges();
        });
    }

    save(): void {
        this.saving = true;
        this._contractService.create(this.contract).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
